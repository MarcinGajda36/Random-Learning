namespace MarcinGajda.EntityFrameworksStuff;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public sealed class DBContextExperiments<TContext>(IDbContextFactory<TContext> dbContextFactory)
   where TContext : DbContext
{
    public Task<TResult> SaveToDbInTransactionAsync<TResult>(
        Func<TContext, CancellationToken, Task<TResult>> resultFactory,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            (isolationLevel, resultFactory),
            static async (context, arguments, cancellationToken) =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                var result = await arguments.resultFactory(context, cancellationToken);
                _ = await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            },
            cancellationToken);
    }

    public Task<TResult> SaveToDbAsync<TResult>(
        Func<TContext, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            resultFactory,
            static async (context, resultFactory, cancellationToken) =>
            {
                var result = await resultFactory(context, cancellationToken);
                _ = await context.SaveChangesAsync(cancellationToken);
                return result;
            },
            cancellationToken);
    }

    public Task<TResult> ReadFromDbAsync<TResult, TEntity>(
        Func<TContext, IQueryable<TEntity>> entitySelector,
        Func<IQueryable<TEntity>, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entitySelector);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            (entitySelector, resultFactory),
            static (context, arguments, cancellationToken) =>
            {
                var entities = arguments.entitySelector(context).AsNoTracking();
                return arguments.resultFactory(entities, cancellationToken);
            },
            cancellationToken);
    }

    public Task<TResult> ReadFromDbInTransactionAsync<TResult, TEntity>(
        Func<TContext, IQueryable<TEntity>> entitySelector,
        Func<IQueryable<TEntity>, CancellationToken, Task<TResult>> resultFactory,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entitySelector);
        ArgumentNullException.ThrowIfNull(resultFactory);
        return ExecuteInStrategyAsync(
            (isolationLevel, entitySelector, resultFactory),
            static async (context, arguments, cancellationToken) =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(arguments.isolationLevel, cancellationToken);
                var entities = arguments.entitySelector(context).AsNoTracking();
                return await arguments.resultFactory(entities, cancellationToken);
            },
            cancellationToken);
    }

    public Task<TResult> ExecuteInStrategyAsync<TArgument, TResult>(
        TArgument argument,
        Func<TContext, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
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
                    contextBase.ChangeTracker.Clear();
                    return arguments.resultFactory((TContext)contextBase, arguments.argument, cancellationToken);
                },
                null,
                cancellationToken);
        }
    }

    public IAsyncEnumerable<TResult> StreamFromDbAsync<TResult>(
        Func<TContext, IQueryable<TResult>> queryFactory,
        CancellationToken cancellationToken = default)
        where TResult : class
    {
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