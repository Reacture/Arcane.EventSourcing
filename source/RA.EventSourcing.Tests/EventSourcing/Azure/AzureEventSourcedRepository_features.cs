﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Idioms;
using Ploeh.AutoFixture.Xunit2;
using ReactiveArchitecture.FakeDomain;
using Xunit;

namespace ReactiveArchitecture.EventSourcing.Azure
{
    public class AzureEventSourcedRepository_features
    {
        public interface IFactory<T>
        {
            T Func(Guid sourceId, IEnumerable<IDomainEvent> pastEvents);
        }

        private IFixture fixture;
        private IAzureEventStore eventStore;
        private IAzureEventPublisher eventPublisher;
        private IAzureEventCorrector eventCorrector;
        private IFactory<FakeUser> factory;
        private AzureEventSourcedRepository<FakeUser> sut;

        public AzureEventSourcedRepository_features()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
            eventStore = Mock.Of<IAzureEventStore>();
            eventPublisher = Mock.Of<IAzureEventPublisher>();
            eventCorrector = Mock.Of<IAzureEventCorrector>();
            factory = Mock.Of<IFactory<FakeUser>>();
            sut = new AzureEventSourcedRepository<FakeUser>(
                eventStore,
                eventPublisher,
                eventCorrector,
                factory.Func);
        }

        [Fact]
        public void sut_implements_IEventSourcedRepositoryT()
        {
            sut.Should().BeAssignableTo<IEventSourcedRepository<FakeUser>>();
        }

        [Fact]
        public void class_has_guard_clauses()
        {
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(AzureEventSourcedRepository<>));
        }

        [Theory]
        [AutoData]
        public async Task Save_saves_events(
            FakeUser user,
            string username)
        {
            user.ChangeUsername(username);
            await sut.Save(user);
            Mock.Get(eventStore).Verify(x => x.SaveEvents<FakeUser>(user.PendingEvents), Times.Once());
        }

        [Theory]
        [AutoData]
        public async Task Save_publishes_events(
            FakeUser user,
            string username)
        {
            user.ChangeUsername(username);
            await sut.Save(user);
            Mock.Get(eventPublisher).Verify(x => x.PublishPendingEvents<FakeUser>(user.Id), Times.Once());
        }

        [Theory]
        [AutoData]
        public void Save_does_not_publish_events_if_fails_to_save(
            FakeUser user,
            string username)
        {
            user.ChangeUsername(username);
            Mock.Get(eventStore)
                .Setup(x => x.SaveEvents<FakeUser>(It.IsAny<IEnumerable<IDomainEvent>>()))
                .Throws<InvalidOperationException>();

            Func<Task> action = () => sut.Save(user);

            action.ShouldThrow<InvalidOperationException>();
            Mock.Get(eventPublisher).Verify(x => x.PublishPendingEvents<FakeUser>(user.Id), Times.Never());
        }

        [Theory]
        [AutoData]
        public async Task Find_corrects_damaged_events(
            FakeUser user,
            string username)
        {
            user.ChangeUsername(username);
            await sut.Find(user.Id);
            Mock.Get(eventCorrector).Verify(x => x.CorrectEvents<FakeUser>(user.Id), Times.Once());
        }

        [Theory]
        [AutoData]
        public async Task Find_restores_aggregate(
            FakeUser user,
            string username)
        {
            // Arrange
            user.ChangeUsername(username);

            Mock.Get(eventStore)
                .Setup(x => x.LoadEvents<FakeUser>(user.Id, 0))
                .ReturnsAsync(user.PendingEvents);

            Mock.Get(factory)
                .Setup(x => x.Func(user.Id, user.PendingEvents))
                .Returns(user);

            // Act
            FakeUser actual = await sut.Find(user.Id);

            // Assert
            actual.Should().BeSameAs(user);
        }

        [Theory]
        [AutoData]
        public void Find_does_not_load_events_if_fails_to_correct_events(
            FakeUser user,
            string username)
        {
            // Arrange
            user.ChangeUsername(username);

            Mock.Get(eventCorrector)
                .Setup(x => x.CorrectEvents<FakeUser>(user.Id))
                .Throws<InvalidOperationException>();

            // Act
            Func<Task> action = () => sut.Find(user.Id);

            // Assert
            action.ShouldThrow<InvalidOperationException>();
            Mock.Get(eventStore).Verify(x => x.LoadEvents<FakeUser>(user.Id, It.IsAny<int>()), Times.Never());
        }

        [Theory]
        [AutoData]
        public async Task Find_returns_null_if_event_not_found(Guid userId)
        {
            Mock.Get(eventStore)
                .Setup(x => x.LoadEvents<FakeUser>(userId, 0))
                .ReturnsAsync(Enumerable.Empty<IDomainEvent>());

            FakeUser actual = await sut.Find(userId);

            actual.Should().BeNull();
            Mock.Get(factory).Verify(x => x.Func(userId, It.IsAny<IEnumerable<IDomainEvent>>()), Times.Never());
        }

        private void RaiseEvents(Guid sourceId, params DomainEvent[] events)
        {
            RaiseEvents(sourceId, 0, events);
        }

        private void RaiseEvents(
            Guid sourceId, int versionOffset, params DomainEvent[] events)
        {
            for (int i = 0; i < events.Length; i++)
            {
                events[i].SourceId = sourceId;
                events[i].Version = versionOffset + i + 1;
                events[i].RaisedAt = DateTimeOffset.Now;
            }
        }
    }
}