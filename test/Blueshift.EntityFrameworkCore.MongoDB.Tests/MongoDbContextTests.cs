﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Blueshift.EntityFrameworkCore.MongoDB.SampleDomain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Xunit;

namespace Blueshift.EntityFrameworkCore.MongoDB.Tests
{
    public class MongoDbContextTests : MongoDbContextTestBase
    {
        [Fact]
        public void Can_query_from_mongodb()
        {
            Assert.Empty(ZooDbContext.Animals.ToList());
            Assert.Empty(ZooDbContext.Employees.ToList());
        }

        [Fact]
        public async void Can_write_simple_record()
        {
            var employee = new Employee { FirstName = "Taiga", LastName = "Masuta", Age = 31.7M };
            ZooDbContext.Add(employee);
            Assert.Equal(1, await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
            Assert.Equal(employee, ZooDbContext.Employees.Single(), new EmployeeEqualityComparer());
        }

        [Fact]
        public async void Can_write_complex_record()
        {
            ZooDbContext.Add(TestEntityFixture.TaigaMasuta);
            Assert.Equal(1, await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
        }

        [Fact]
        public async void Can_query_complex_record()
        {
            ZooDbContext.Add(TestEntityFixture.TaigaMasuta);
            Assert.Equal(1, await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
            Assert.Equal(TestEntityFixture.TaigaMasuta, await ZooDbContext.Employees
                .SingleAsync(searchedEmployee => searchedEmployee.Specialties
                    .Any(speciality => speciality.Task == ZooTask.Feeding)), new EmployeeEqualityComparer());
        }

        [Fact]
        public async void Can_write_polymorphic_records()
        {
            ZooDbContext.Animals.AddRange(TestEntityFixture.Animals);
            Assert.Equal(
                TestEntityFixture.Animals.Count + TestEntityFixture.Enclosures.Count,
                await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
            IList<Animal> queriedEntities = ZooDbContext.Animals
                .OrderBy(animal => animal.Name)
                .ThenBy(animal => animal.Height)
                .ToList();
            Assert.Equal(TestEntityFixture.Animals, queriedEntities, new AnimalEqualityComparer());
        }

        [Fact]
        public async void Can_query_polymorphic_sub_types()
        {
            ZooDbContext.Animals.AddRange(TestEntityFixture.Animals);
            Assert.Equal(
                TestEntityFixture.Animals.Count + TestEntityFixture.Enclosures.Count,
                await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));

            Assert.Equal(
                TestEntityFixture.Animals.OfType<Tiger>().Single(),
                ZooDbContext.Animals.OfType<Tiger>().Single(),
                new AnimalEqualityComparer());
            Assert.Equal(
                TestEntityFixture.Animals.OfType<PolarBear>().Single(),
                ZooDbContext.Animals.OfType<PolarBear>().Single(),
                new AnimalEqualityComparer());
            Assert.Equal(
                TestEntityFixture.Animals.OfType<SeaOtter>().Single(),
                ZooDbContext.Animals.OfType<SeaOtter>().Single(),
                new AnimalEqualityComparer());
            Assert.Equal(
                TestEntityFixture.Animals.OfType<EurasianOtter>().Single(),
                ZooDbContext.Animals.OfType<EurasianOtter>().Single(),
                new AnimalEqualityComparer());
            IList<Otter> insertedOtters = TestEntityFixture.Animals
                .OfType<Otter>()
                .OrderBy(otter => otter.Name)
                .ToList();
            IList<Otter> queriedOtters = ZooDbContext.Animals
                .OfType<Otter>()
                .OrderBy(otter => otter.Name)
                .ToList();
            Assert.Equal(insertedOtters, queriedOtters, new AnimalEqualityComparer());
        }

        [Fact]
        public async void Can_list_async()
        {
            ZooDbContext.Animals.AddRange(TestEntityFixture.Animals);
            Assert.Equal(
                TestEntityFixture.Animals.Count + TestEntityFixture.Enclosures.Count,
                await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
            Assert.Equal(TestEntityFixture.Animals,
                await ZooDbContext.Animals
                    .OrderBy(animal => animal.Name)
                    .ThenBy(animal => animal.Height)
                    .ToListAsync(),
                new AnimalEqualityComparer());
        }

        [Fact]
        public async void Can_list_async_with_include()
        {
            ZooDbContext.Animals.AddRange(TestEntityFixture.Animals);
            Assert.Equal(
                TestEntityFixture.Animals.Count + TestEntityFixture.Enclosures.Count,
                await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
            IEnumerable<Animal> queriedAnimals = await ZooDbContext.Animals
                .Include(animal => animal.Enclosure)
                .OrderBy(animal => animal.Name)
                .ThenBy(animal => animal.Age)
                .ToListAsync();
            Assert.Equal(TestEntityFixture.Animals,
                queriedAnimals,
                new AnimalWithEnclosureEqualityComparer());
            Assert.Equal(TestEntityFixture.Enclosures,
                queriedAnimals
                    .Select(animal => animal.Enclosure)
                    .Distinct(new EnclosureEqualityComparer())
                    .OrderBy(enclosure => enclosure.AnimalEnclosureType)
                    .ThenBy(enclosure => enclosure.Name)
                    .ToList(),
                new EnclosureEqualityComparer());
        }

        [Fact]
        public async void Can_query_with_included_enumerable_property()
        {
            ZooDbContext.Enclosures.AddRange(TestEntityFixture.Enclosures);
            Assert.Equal(
                TestEntityFixture.Enclosures.Count + TestEntityFixture.Animals.Count,
                await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
            IEnumerable<Enclosure> queriedEnclosures = await ZooDbContext.Enclosures
                .Include(enclosure => enclosure.Animals)
                .OrderBy(enclosure => enclosure.AnimalEnclosureType)
                .ThenBy(enclosure => enclosure.Name)
                .ToListAsync();
            Assert.Equal(TestEntityFixture.Enclosures,
                queriedEnclosures,
                new EnclosureWithAnimalsEqualityComparer());
            Assert.Equal(TestEntityFixture.Animals,
                queriedEnclosures
                    .SelectMany(enclosure => enclosure.Animals)
                    .Distinct(EqualityComparer<Animal>.Default)
                    .OrderBy(animal => animal.Name)
                    .ThenBy(animal => animal.Height)
                    .ToList(),
                new AnimalWithEnclosureEqualityComparer());
        }

        [Fact]
        public async void Can_query_first_or_default_async()
        {
            ZooDbContext.Animals.AddRange(TestEntityFixture.Animals);
            Assert.Equal(
                TestEntityFixture.Animals.Count + TestEntityFixture.Enclosures.Count,
                await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
            Assert.Equal(
                TestEntityFixture.Animals.OfType<Tiger>().Single(),
                await ZooDbContext.Animals.OfType<Tiger>().FirstOrDefaultAsync(),
                new AnimalEqualityComparer());
        }

        [Fact]
        public async Task Can_update_existing_entity()
        {
            EntityEntry entityEntry = ZooDbContext.Add(TestEntityFixture.Tigger);
            Assert.Equal(EntityState.Added, entityEntry.State);
            Assert.Equal(2, await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
            Assert.Equal(EntityState.Unchanged, entityEntry.State);
            Assert.NotNull(TestEntityFixture.Tigger.ConcurrencyField);

            Assert.NotNull(entityEntry.OriginalValues[nameof(TestEntityFixture.Tigger.ConcurrencyField)]);

            TestEntityFixture.Tigger.Name = "Tigra";
            ZooDbContext.ChangeTracker.DetectChanges();
            Assert.Equal(EntityState.Modified, entityEntry.State);
            Assert.Equal(1, await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
        }

        [Fact]
        public async void Concurrency_field_prevents_updates()
        {
            EntityEntry entityEntry = ZooDbContext.Add(TestEntityFixture.Tigger);
            Assert.Equal(2, await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
            Assert.False(string.IsNullOrWhiteSpace(TestEntityFixture.Tigger.ConcurrencyField));

            string newConcurrencyToken = Guid.NewGuid().ToString();
            entityEntry.Property(nameof(Animal.ConcurrencyField)).OriginalValue = newConcurrencyToken;
            typeof(Animal)
                .GetTypeInfo()
                .GetProperty(nameof(Animal.ConcurrencyField))
                .SetValue(TestEntityFixture.Tigger, newConcurrencyToken);
            Assert.Equal(0, await ZooDbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true));
        }
    }
}