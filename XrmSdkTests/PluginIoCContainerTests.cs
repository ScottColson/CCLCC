﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCLLC.Xrm.Sdk;
using CCLLC.Xrm.Sdk.Configuration;
using CCLLC.Xrm.Sdk.Encryption;
using CCLLC.Xrm.Sdk.Utilities;
using CCLLC.Telemetry;
using CCLLC.Telemetry.EventLogger;
using CCLLC.Telemetry.Context;
using CCLLC.Telemetry.Client;
using CCLLC.Telemetry.Sink;
using CCLLC.Telemetry.Serializer;
using TestHelpers;

namespace XrmSdkTests
{
    [TestClass]
    public class PluginIoCContainerTests
    {          
        [TestMethod]
        public void InstrumentedPlugin_Container_IsNot_Plugin_Container()
        {
            var plugin1 = new Plugin(null, null);
            var plugin2 = new InstrumentedPlugin(null, null);

            Assert.IsNotNull(plugin1.Container);
            Assert.IsNotNull(plugin2.Container);

            Assert.AreNotSame(plugin1.Container, plugin2.Container);

        }

        [TestMethod]
        public void Plugin_Container_Initialization()
        {
            var plugin = new Plugin(null, null);
            Assert.IsNotNull(plugin.Container);
            Assert.AreEqual(6,plugin.Container.Count);

            //verify expected concreate implementations for each registered interface.
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ICacheFactory, CacheFactory>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IConfigurationFactory, ConfigurationFactory>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ILocalPluginContextFactory, LocalPluginContextFactory>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IRijndaelEncryption, RijndaelEncryption>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IExtensionSettingsConfig, DefaultExtensionSettingsConfig>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IPluginWebRequestFactory, PluginHttpWebRequestFactory>());
        }

        [TestMethod]
        public void InstrumentedPlugin_Container_Initialization()
        {
            var plugin = new InstrumentedPlugin(null, null, false); //do not override channel
            Assert.IsNotNull(plugin.Container);
            Assert.AreEqual(20, plugin.Container.Count);

            //verify expected concreate implementations for each registered interface.
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ICacheFactory, CacheFactory>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IConfigurationFactory, ConfigurationFactory>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ILocalPluginContextFactory, LocalPluginContextFactory>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IRijndaelEncryption, RijndaelEncryption>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IExtensionSettingsConfig, DefaultExtensionSettingsConfig>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IPluginWebRequestFactory, PluginHttpWebRequestFactory>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IXrmTelemetryPropertyManager, CCLLC.Xrm.Sdk.Telemetry.ExecutionContextPropertyManager>(true));
            
            //verify expected concrete implementation for telemetry support
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IEventLogger,InertEventLogger>(true));
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ITelemetryFactory,TelemetryFactory>(true));            
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ITelemetryClientFactory,TelemetryClientFactory>(true));            
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ITelemetryContext, TelemetryContext>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ITelemetryInitializerChain, TelemetryInitializerChain>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ITelemetrySink,TelemetrySink>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ITelemetryProcessChain,TelemetryProcessChain>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ITelemetryChannel, SyncMemoryChannel>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ITelemetryBuffer, TelemetryBuffer>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ITelemetryTransmitter, AITelemetryTransmitter>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IContextTagKeys,AIContextTagKeys>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<ITelemetrySerializer,AITelemetrySerializer>());
            Assert.IsTrue(plugin.Container.IsRegisteredAs<IJsonWriterFactory, JsonWriterFactory>());
        }
        
    }
}
