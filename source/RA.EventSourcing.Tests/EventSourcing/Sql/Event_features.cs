﻿using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Idioms;
using ReactiveArchitecture.Messaging;

namespace ReactiveArchitecture.EventSourcing.Sql
{
    [TestClass]
    public class Event_features
    {
        public class FakeDomainEvent : DomainEvent
        {
            public Guid GuidProp { get; set; }

            public int Int32Prop { get; set; }

            public double DoubleProp { get; set; }

            public string StringProp { get; set; }
        }

        private IFixture fixture =
            new Fixture().Customize(new AutoMoqCustomization());

        [TestMethod]
        public void FromEnvelope_has_guard_clauses()
        {
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(Event).GetMethod("FromEnvelope"));
        }

        [TestMethod]
        public void FromEnvelope_has_guard_clause_for_invalid_message()
        {
            var envelope = new Envelope(new object());
            Action action = () => Event.FromEnvelope(envelope, new JsonMessageSerializer());
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "envelope");
        }

        [TestMethod]
        public void FromEnvelope_sets_AggregatId_correctly()
        {
            var domainEvent = fixture.Create<FakeDomainEvent>();
            var envelope = new Envelope(domainEvent);
            var actual = Event.FromEnvelope(
                envelope, new JsonMessageSerializer());
            actual.AggregateId.Should().Be(domainEvent.SourceId);
        }

        [TestMethod]
        public void FromEnvelope_sets_Version_correctly()
        {
            var domainEvent = fixture.Create<FakeDomainEvent>();
            var envelope = new Envelope(domainEvent);
            var actual = Event.FromEnvelope(
                envelope, new JsonMessageSerializer());
            actual.Version.Should().Be(domainEvent.Version);
        }

        [TestMethod]
        public void FromEnvelope_sets_EventType_correctly()
        {
            var domainEvent = fixture.Create<FakeDomainEvent>();
            var envelope = new Envelope(domainEvent);
            var actual = Event.FromEnvelope(
                envelope, new JsonMessageSerializer());
            actual.EventType.Should().Be(typeof(FakeDomainEvent).FullName);
        }

        [TestMethod]
        public void FromEnvelope_sets_MessageId_correctly()
        {
            var domainEvent = fixture.Create<FakeDomainEvent>();
            var envelope = new Envelope(domainEvent);
            var actual = Event.FromEnvelope(
                envelope, new JsonMessageSerializer());
            actual.MessageId.Should().Be(envelope.MessageId);
        }

        [TestMethod]
        public void FromEnvelope_sets_CorrelationId_correctly()
        {
            var domainEvent = fixture.Create<FakeDomainEvent>();
            var correlationId = Guid.NewGuid();
            var envelope = new Envelope(correlationId, domainEvent);
            var actual = Event.FromEnvelope(
                envelope, new JsonMessageSerializer());
            actual.CorrelationId.Should().Be(correlationId);
        }

        [TestMethod]
        public void FromEnvelope_sets_PayloadJson_correctly()
        {
            var serializer = new JsonMessageSerializer();
            var domainEvent = fixture.Create<FakeDomainEvent>();
            var envelope = new Envelope(domainEvent);

            var actual = Event.FromEnvelope(envelope, serializer);

            object deserialized = serializer.Deserialize(actual.PayloadJson);
            deserialized.Should().BeOfType<FakeDomainEvent>();
            deserialized.ShouldBeEquivalentTo(domainEvent);
        }

        [TestMethod]
        public void FromEnvelope_sets_RaisedAt_correctly()
        {
            var domainEvent = fixture.Create<FakeDomainEvent>();
            var envelope = new Envelope(domainEvent);
            var actual = Event.FromEnvelope(
                envelope, new JsonMessageSerializer());
            actual.RaisedAt.Should().Be(domainEvent.RaisedAt);
        }
    }
}
