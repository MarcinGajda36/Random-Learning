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
            async (context, cancellationToken) =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
                var result = await resultFactory(context, cancellationToken);
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
            async (context, cancellationToken) =>
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
            (context, cancellationToken) =>
            {
                var entities = entitySelector(context).AsNoTracking();
                return resultFactory(entities, cancellationToken);
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
            async (context, cancellationToken) =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
                var entities = entitySelector(context).AsNoTracking();
                return await resultFactory(entities, cancellationToken);
            },
            cancellationToken);
    }

    public Task<TResult> ExecuteInStrategyAsync<TResult>(
        Func<TContext, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultFactory);
        return Core(resultFactory, cancellationToken);

        async Task<TResult> Core(
            Func<TContext, CancellationToken, Task<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var strategy = context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(
                resultFactory,
                (contextBase, resultFactory, cancellationToken) =>
                {
                    contextBase.ChangeTracker.Clear();
                    var context = (TContext)contextBase;
                    return resultFactory(context, cancellationToken);
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
        return Core(queryFactory, cancellationToken);

        async IAsyncEnumerable<TResult> Core(
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