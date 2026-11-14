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

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace MongoDB.EntityFrameworkCore.Metadata.Conventions;

/// <summary>
/// The join entity type of a skip navigation is referenced by the entity types on both sides of the many-to-many relationship,
/// so it cannot be embedded in a single owner's document. This makes it an independent entity type if it was embedded by
/// convention, for example because another type holds a collection of it.
/// </summary>
internal sealed class MongoJoinEntityTypeConvention : ISkipNavigationForeignKeyChangedConvention
{
    /// <inheritdoc />
    public void ProcessSkipNavigationForeignKeyChanged(
        IConventionSkipNavigationBuilder skipNavigationBuilder,
        IConventionForeignKey? foreignKey,
        IConventionForeignKey? oldForeignKey,
        IConventionContext<IConventionForeignKey> context)
    {
        if (foreignKey != null)
        {
            OwnedTypeDemotion.Demote(foreignKey.DeclaringEntityType);
        }
    }
}
