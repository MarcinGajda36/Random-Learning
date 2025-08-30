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
        => SaveInTransactionAsync(
            dbContextFactory,
            resultFactory,
            static (context, resultFactory, cancellationToken) => resultFactory(context, cancellationToken),
            isolationLevel,
            cancellationToken);

    public static Task<TResult> SaveInTransactionAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (isolationLevel, resultFactory, argument),
            static async (context, arguments, cancellationToken) =>
            {
                var (isolationLevel, resultFactory, argument) = arguments;
                await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
                var result = await resultFactory(context, argument, cancellationToken);
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
        => SaveAsync(
            dbContextFactory,
            resultFactory,
            static (context, resultFactory, cancellationToken) => resultFactory(context, cancellationToken),
            cancellationToken);

    public static Task<TResult> SaveAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (resultFactory, argument),
            static async (context, arguments, cancellationToken) =>
            {
                var (resultFactory, argument) = arguments;
                var result = await resultFactory(context, argument, cancellationToken);
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
        => ReadAsync(
            dbContextFactory,
            resultFactory,
            entitySelector,
            static (entities, resultFactory, cancellationToken) => resultFactory(entities, cancellationToken),
            cancellationToken);

    public static Task<TResult> ReadAsync<TContext, TArgument, TResult, TEntity>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, IQueryable<TEntity>> entitySelector,
        Func<IQueryable<TEntity>, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(entitySelector);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            dbContextFactory,
            (entitySelector, resultFactory, argument),
            static (context, arguments, cancellationToken) =>
            {
                var (entitySelector, resultFactory, argument) = arguments;
                var entities = entitySelector(context).AsNoTracking();
                return resultFactory(entities, argument, cancellationToken);
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
        => ReadInTransactionAsync(
            dbContextFactory,
            resultFactory,
            entitySelector,
            static (queryable, resultSelector, cancellationToken) => resultSelector(queryable, cancellationToken),
            isolationLevel,
            cancellationToken);

    public static Task<TResult> ReadInTransactionAsync<TContext, TArgument, TResult, TEntity>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, IQueryable<TEntity>> entitySelector,
        Func<IQueryable<TEntity>, TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
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
            (isolationLevel, entitySelector, resultFactory, argument),
            static async (context, arguments, cancellationToken) =>
            {
                var (isolationLevel, entitySelector, resultFactory, argument) = arguments;
                await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
                var entities = entitySelector(context).AsNoTracking();
                return await resultFactory(entities, argument, cancellationToken);
            },
            cancellationToken);
    }

    public static Task<TResult> ExecuteInStrategyAsync<TContext, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        Func<TContext, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => ExecuteInStrategyAsync(
            dbContextFactory,
            resultFactory,
            static (context, resultFactory, cancellationToken) => resultFactory(context, cancellationToken),
            cancellationToken);

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
        => StreamAsync(
            dbContextFactory,
            queryFactory,
            static (context, queryFactory) => queryFactory(context),
            cancellationToken);

    public static IAsyncEnumerable<TResult> StreamAsync<TContext, TArgument, TResult>(
        this IDbContextFactory<TContext> dbContextFactory,
        TArgument argument,
        Func<TContext, TArgument, IQueryable<TResult>> queryFactory,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(queryFactory);
        return Core(dbContextFactory, argument, queryFactory, cancellationToken);

        static async IAsyncEnumerable<TResult> Core(
            IDbContextFactory<TContext> dbContextFactory,
            TArgument argument,
            Func<TContext, TArgument, IQueryable<TResult>> queryFactory,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await foreach (var result in queryFactory(context, argument)
                .AsNoTracking()
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken))
            {
                yield return result;
            }
        }
    }
}