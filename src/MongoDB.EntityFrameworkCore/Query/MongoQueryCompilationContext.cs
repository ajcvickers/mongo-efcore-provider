/* Copyright 2023-present MongoDB Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace MongoDB.EntityFrameworkCore.Query;

/// <inheritdoc />
public class MongoQueryCompilationContext(QueryCompilationContextDependencies dependencies, bool async, bool useNativeQuery = true)
    : QueryCompilationContext(dependencies, async)
{
    /// <summary>
    /// The original expression that was passed to the query translator.
    /// </summary>
    public Expression? OriginalExpression { get; internal set; }

    /// <summary>
    /// Whether the native MQL query path is enabled for queries compiled in this context, as configured by
    /// the per-context <c>UseNativeQuery</c> option.
    /// </summary>
    public bool UseNativeQuery { get; } = useNativeQuery;

    /// <inheritdoc/>
    public override Func<QueryContext, TResult> CreateQueryExecutor<TResult>(Expression query)
    {
        OriginalExpression = query;
        return base.CreateQueryExecutor<TResult>(query);
    }
}
