namespace MarcinGajda.EntityFrameworksStuff;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public static class DbContextFactoryExtensions
{
    public static Task<TResult> SaveInTransactionAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (isolationLevel, resultFactory),
            static async (context, arguments, cancellationToken) =>
            {
                var (isolationLevel, resultFactory) = arguments;
                await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
                var result = await resultFactory(context, cancellationToken);
                _ = await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            },
            cancellationToken);
    }

    public static Task<TResult> SaveAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            resultFactory,
            static async (context, resultFactory, cancellationToken) =>
            {
                var result = await resultFactory(context, cancellationToken);
                _ = await context.SaveChangesAsync(cancellationToken);
                return result;
            },
            cancellationToken);
    }

    public static Task<TResult> ReadAsync<TContext, TResult, TEntity>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, IQueryable<TEntity>> entitySelector,
        Func<IQueryable<TEntity>, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(entitySelector);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (entitySelector, resultFactory),
            static (context, arguments, cancellationToken) =>
            {
                var (entitySelector, resultFactory) = arguments;
                var entities = entitySelector(context).AsNoTracking();
                return resultFactory(entities, cancellationToken);
            },
            cancellationToken);
    }

    public static Task<TResult> ReadInTransactionAsync<TContext, TResult, TEntity>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, IQueryable<TEntity>> entitySelector,
        Func<IQueryable<TEntity>, CancellationToken, ValueTask<TResult>> resultFactory,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(entitySelector);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (isolationLevel, entitySelector, resultFactory),
            static async (context, arguments, cancellationToken) =>
            {
                var (isolationLevel, entitySelector, resultFactory) = arguments;
                await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
                var entities = entitySelector(context).AsNoTracking();
                return await resultFactory(entities, cancellationToken);
            },
            cancellationToken);
    }

    public static Task<TResult> ExecuteInStrategyAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return Core(dbContextFactory, argument, resultFactory, cancellationToken);

        static async Task<TResult> Core(
            IDbContextFactory<TContext> dbContextFactory,
            TArgument argument,
            Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(
                (argument, resultFactory),
                static (contextBase, arguments, cancellationToken) =>
                {
                    var (argument, resultFactory) = arguments;
                    contextBase.ChangeTracker.Clear(); // When re-try triggers then ChangeTracker still has changes from previous try iirc.
                    return resultFactory((TContext)contextBase, argument, cancellationToken);
                },
                null,
                cancellationToken);
        }
    }

    public static IAsyncEnumerable<TResult> StreamAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, IQueryable<TResult>> queryFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(queryFactory);
        return Core(dbContextFactory, queryFactory, cancellationToken);

        static async IAsyncEnumerable<TResult> Core(
            IDbContextFactory<TContext> dbContextFactory,
            Func<TContext, IQueryable<TResult>> queryFactory,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await foreach (var result in queryFactory(context)
                .AsNoTracking()
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken))
            {
                yield return result;
            }
        }
    }
}