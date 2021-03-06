﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Linq;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaProducerFactoryTest
    {
        private readonly IConfigurationRoot emptyConfiguration;

        public KafkaProducerFactoryTest()
        {
            this.emptyConfiguration = new ConfigurationBuilder().Build();
        }

        [Fact]
        public void When_No_Type_Is_Set_Should_Create_ByteArray_Producer()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var producer = factory.Create(entity);

            Assert.NotNull(producer);
            Assert.IsType<KafkaProducer<Null, byte[]>>(producer);
            var typedProducer = (KafkaProducer<Null, byte[]>)producer;
            Assert.Null(typedProducer.ValueSerializer);
        }

        [Fact]
        public void When_String_Value_Type_Is_Set_Should_Create_String_Listener()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(string),
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var producer = factory.Create(entity);

            Assert.NotNull(producer);
            Assert.IsType<KafkaProducer<Null, string>>(producer);
            var typedProducer = (KafkaProducer<Null, string>)producer;
            Assert.Null(typedProducer.ValueSerializer);
        }

        [Fact]
        public void When_Avro_Schema_Is_Provided_Should_Create_GenericRecord_Listener()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
                AvroSchema = "fakeAvroSchema"
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(GenericRecord),
                AvroSchema = attribute.AvroSchema,
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var producer = factory.Create(entity);

            Assert.NotNull(producer);
            Assert.IsType<KafkaProducer<Null, GenericRecord>>(producer);
            var typedProducer = (KafkaProducer<Null, GenericRecord>)producer;
            Assert.NotNull(typedProducer.ValueSerializer);
            Assert.IsType<AvroSerializer<GenericRecord>>(typedProducer.ValueSerializer);
        }


        [Fact]
        public void When_Value_Type_Is_Specific_Record_Should_Create_SpecificRecord_Listener()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
            };


            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(MyAvroRecord),
                AvroSchema = MyAvroRecord.SchemaText,
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var producer = factory.Create(entity);

            Assert.NotNull(producer);
            Assert.IsType<KafkaProducer<Null, MyAvroRecord>>(producer);
            var typedProducer = (KafkaProducer<Null, MyAvroRecord>)producer;
            Assert.NotNull(typedProducer.ValueSerializer);
            Assert.IsType<AvroSerializer<MyAvroRecord>>(typedProducer.ValueSerializer);
        }

        [Fact]
        public void When_Value_Type_Is_Protobuf_Should_Create_Protobuf_Listener()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(ProtoUser),
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var producer = factory.Create(entity);

            Assert.NotNull(producer);
            Assert.IsType<KafkaProducer<Null, ProtoUser>>(producer);
            var typedProducer = (KafkaProducer<Null, ProtoUser>)producer;
            Assert.NotNull(typedProducer.ValueSerializer);
            Assert.IsType<ProtobufSerializer<ProtoUser>>(typedProducer.ValueSerializer);
        }

        [Fact]
        public void GetProducerConfig_When_No_Auth_Defined_Should_Contain_Only_BrokerList()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(ProtoUser),
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var config = factory.GetProducerConfig(entity);
            Assert.Single(config);
            Assert.Equal("brokers:9092", config.BootstrapServers);
        }

        [Fact]
        public void GetProducerConfig_When_Auth_Defined_Should_Contain_Them()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
                AuthenticationMode = BrokerAuthenticationMode.Plain,
                Protocol = BrokerProtocol.SaslSsl,
                Username = "myuser",
                Password = "secret",
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(ProtoUser),
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var config = factory.GetProducerConfig(entity);
            Assert.Equal(5, config.Count());
            Assert.Equal("brokers:9092", config.BootstrapServers);
            Assert.Equal(SecurityProtocol.SaslSsl, config.SecurityProtocol);
            Assert.Equal(SaslMechanism.Plain, config.SaslMechanism);
            Assert.Equal("secret", config.SaslPassword);
            Assert.Equal("myuser", config.SaslUsername);
        }

        [Fact]
        public void GetProducerConfig_When_Ssl_Auth_Defined_Should_Contain_Them()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
                Protocol = BrokerProtocol.Ssl,
                SslKeyLocation = "path/to/key",
                SslKeyPassword = "password1",
                SslCertificateLocation = "path/to/cert",
                SslCaLocation = "path/to/cacert"
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(ProtoUser),
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var config = factory.GetProducerConfig(entity);
            Assert.Equal(6, config.Count());
            Assert.Equal("brokers:9092", config.BootstrapServers);
            Assert.Equal(SecurityProtocol.Ssl, config.SecurityProtocol);
            Assert.Equal("path/to/key", config.SslKeyLocation);
            Assert.Equal("password1", config.SslKeyPassword);
            Assert.Equal("path/to/cert", config.SslCertificateLocation);
            Assert.Equal("path/to/cacert", config.SslCaLocation);
        }
    }
}