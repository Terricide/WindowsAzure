﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.WindowsAzure.Table;
using GitHub.WindowsAzure.Table.Extensions;
using GitHub.WindowsAzure.Tests.Samples;
using Xunit;

namespace GitHub.WindowsAzure.Tests.Table.Context.Query
{
    public sealed class ComplexQueryTests : TableSetTestBase
    {
        private const string Germany = "Germany";
        private const string Spain = "Spain";
        private const string Finland = "Finland";
        private const string France = "France";

        public ComplexQueryTests()
        {
            TableSet<Country> tableSet = GetTableSet();
            tableSet.Add(
                new Country
                    {
                        Area = 357021,
                        Continent = "Europe",
                        TopSecretKey = new byte[] {0xaa, 0xbb, 0xcc},
                        Formed = new DateTime(1871, 1, 18),
                        Id = Guid.NewGuid(),
                        IsExists = true,
                        Name = Germany,
                        Population = 81799600,
                        PresidentsCount = 11
                    });
            tableSet.Add(
                new Country
                    {
                        Area = 505992,
                        Continent = "Europe",
                        TopSecretKey = new byte[] {0xaa, 0xbb, 0xcc},
                        Formed = new DateTime(1812, 1, 1),
                        Id = Guid.NewGuid(),
                        IsExists = false,
                        Name = Spain,
                        Population = 47190493,
                        PresidentsCount = 8
                    });
            tableSet.Add(
                new Country
                    {
                        Area = 674843,
                        Continent = "Europe",
                        TopSecretKey = new byte[] {0xaa, 0xbb, 0xcc},
                        Formed = new DateTime(1792, 1, 1),
                        Id = Guid.NewGuid(),
                        IsExists = true,
                        Name = France,
                        Population = 65350000,
                        PresidentsCount = 24
                    });
            tableSet.Add(
                new Country
                    {
                        Area = 338424,
                        Continent = "Europe",
                        TopSecretKey = new byte[] {0xaa, 0xbb, 0xcc},
                        Formed = new DateTime(1809, 3, 29),
                        Id = Guid.NewGuid(),
                        IsExists = true,
                        Name = Finland,
                        Population = 5421827,
                        PresidentsCount = 12
                    });
        }

        [Fact]
        public async Task QueryWithMultipleScopesTest()
        {
            // Arrange
            const int value = 1800;
            TableSet<Country> tableSet = GetTableSet();

            // Act
            IQueryable<Country> query =
                tableSet.Where(
                    p => p.Formed > new DateTime(value, 1, 1) &&
                         (p.PresidentsCount < 10 ||
                          p.Population < 10000000 && p.PresidentsCount > 10 && p.IsExists));

            List<Country> values = await query.ToListAsync();

            // Assert
            Assert.Equal(values.Count, 2);
            Assert.Contains(Finland, values.Select(p => p.Name));
            Assert.Contains(Spain, values.Select(p => p.Name));
        }
    }
}