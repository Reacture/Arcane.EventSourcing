﻿namespace Khala.EventSourcing.Sql
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using FluentAssertions;
    using Khala.Messaging;
    using Xunit;

    public class PendingEvent_specs
    {
        [Fact]
        public void sut_has_AggregateId_property()
        {
            typeof(PendingEvent).Should().HaveProperty<Guid>("AggregateId");
        }

        [Fact]
        public void AggregateId_is_decorated_with_Key()
        {
            typeof(PendingEvent)
                .GetProperty("AggregateId")
                .Should()
                .BeDecoratedWith<KeyAttribute>();
        }

        [Fact]
        public void AggregateId_is_decorated_with_Column()
        {
            typeof(PendingEvent)
                .GetProperty("AggregateId")
                .Should()
                .BeDecoratedWith<ColumnAttribute>(a => a.Order == 0);
        }

        [Fact]
        public void sut_has_Version_property()
        {
            typeof(PendingEvent).Should().HaveProperty<int>("Version");
        }

        [Fact]
        public void Version_is_decorated_with_Key()
        {
            typeof(PendingEvent)
                .GetProperty("Version")
                .Should()
                .BeDecoratedWith<KeyAttribute>();
        }

        [Fact]
        public void Version_is_decorated_with_Column()
        {
            typeof(PendingEvent)
                .GetProperty("Version")
                .Should()
                .BeDecoratedWith<ColumnAttribute>(a => a.Order == 1);
        }

        [Fact]
        public void sut_has_MessageId_property()
        {
            typeof(PendingEvent).Should().HaveProperty<Guid>("MessageId");
        }

        [Fact]
        public void sut_has_CorrelationId_property()
        {
            typeof(PendingEvent).Should().HaveProperty<Guid?>("CorrelationId");
        }

        [Fact]
        public void sut_has_EventJson_property()
        {
            typeof(PendingEvent).Should().HaveProperty<string>("EventJson");
        }

        [Fact]
        public void EventJson_is_decorated_with_Required()
        {
            typeof(PendingEvent)
                .GetProperty("EventJson")
                .Should()
                .BeDecoratedWith<RequiredAttribute>(a => a.AllowEmptyStrings == false);
        }

        [Fact]
        public void FromEnvelope_generates_PendingEvent_correctly()
        {
            var domainEvent = new SomeDomainEvent();
            var messageId = Guid.NewGuid();
            var correlationId = Guid.NewGuid();
            var envelope = new Envelope(messageId, correlationId, domainEvent);
            IMessageSerializer serializer = new JsonMessageSerializer();

            var actual = PendingEvent.FromEnvelope(envelope, serializer);

            actual.AggregateId.Should().Be(domainEvent.SourceId);
            actual.Version.Should().Be(domainEvent.Version);
            actual.MessageId.Should().Be(messageId);
            actual.CorrelationId.Should().Be(correlationId);
            actual.EventJson.Should().Be(serializer.Serialize(domainEvent));
        }

        [Fact]
        public void FromEnvelope_has_guard_clause_for_invalid_message()
        {
            var envelope = new Envelope(new object());
            Action action = () => PendingEvent.FromEnvelope(envelope, new JsonMessageSerializer());
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "envelope");
        }

        public class SomeDomainEvent : DomainEvent
        {
            private static Random _random = new Random();

            public SomeDomainEvent()
            {
                SourceId = Guid.NewGuid();
                Version = _random.Next();
                RaisedAt = DateTimeOffset.Now.AddTicks(_random.Next());
            }

            public string Content { get; set; } = Guid.NewGuid().ToString();
        }
    }
}