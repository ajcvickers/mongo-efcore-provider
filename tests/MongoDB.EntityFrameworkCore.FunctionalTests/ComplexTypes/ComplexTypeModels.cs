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

using MongoDB.Bson;

namespace MongoDB.EntityFrameworkCore.FunctionalTests.ComplexTypes;

#nullable enable

public struct GeoPoint
{
    public double Lat { get; set; }
    public double Lon { get; set; }
}

public class ComplexAddress
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public GeoPoint Location { get; set; }
}

public class CustomerWithAddress
{
    public ObjectId Id { get; set; }
    public string Name { get; set; } = null!;
    public ComplexAddress Address { get; set; } = null!;
}

#if !EF8 && !EF9
public class CustomerWithOptionalAddress
{
    public ObjectId Id { get; set; }
    public string Name { get; set; } = null!;
    public ComplexAddress? Address { get; set; }
}

public class CustomerWithAddressList
{
    public ObjectId Id { get; set; }
    public string Name { get; set; } = null!;
    public List<ComplexAddress> Addresses { get; set; } = [];
}
#endif
