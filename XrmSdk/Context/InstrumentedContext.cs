﻿using System;
using CCLLC.Core;
using CCLLC.Telemetry;
using Microsoft.Xrm.Sdk;

namespace CCLLC.Xrm.Sdk.Context
{
    public abstract class InstrumentedContext : LocalContext, ISupportContextInstrumentation
    {
        public IComponentTelemetryClient TelemetryClient { get; private set; }

        private ITelemetryFactory _telemetryFactory;
        public ITelemetryFactory TelemetryFactory
        {
            get
            {
                if (_telemetryFactory == null)
                {
                    _telemetryFactory = this.Container.Resolve<ITelemetryFactory>();
                }
                return _telemetryFactory;
            }
        }

        protected internal InstrumentedContext(IExecutionContext executionContext, IIocContainer container, IComponentTelemetryClient telemetryClient) : base(executionContext, container)
        {
            if(telemetryClient == null) { throw new ArgumentNullException("telemetryClient"); }
            this.TelemetryClient = telemetryClient;
        }

        public override IPluginWebRequest CreateWebRequest(Uri address, string dependencyName = null)
        {
            return WebRequestFactory.BuildPluginWebRequest(address, dependencyName,  this.TelemetryFactory, this.TelemetryClient);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.TelemetryClient != null)
                {
                    if (this.TelemetryClient.TelemetrySink != null)
                    {
                        this.TelemetryClient.TelemetrySink.OnConfigure = null;
                    }
                    this.TelemetryClient.Dispose();

                    this.TelemetryClient = null;
                    this._telemetryFactory = null;
                }
            }

            base.Dispose(disposing);

        }
               
        public virtual void SetAlternateDataKey(string name, string value)
        {
            if (this.TelemetryClient != null)
            {
                var asDataContext = this.TelemetryClient.Context as ISupportDataKeyContext;
                if (asDataContext != null)
                {
                    asDataContext.Data.AltKeyName = name;
                    asDataContext.Data.AltKeyValue = value;
                }
                else
                {
                    TelemetryClient.Context.Properties["alternate-key-name"] = name;
                    TelemetryClient.Context.Properties["alternate-key-value"] = value;
                }
            }
        }

        public override void Trace(eMessageType type, string message, params object[] args)
        {
            base.Trace(type, message, args);
            if (!string.IsNullOrEmpty(message))
            {
                if (this.TelemetryClient != null && this.TelemetryFactory != null)
                {
                    var level = eSeverityLevel.Information;
                    if (type == eMessageType.Warning)
                    {
                        level = eSeverityLevel.Warning;
                    }
                    else if (type == eMessageType.Error)
                    {
                        level = eSeverityLevel.Error;
                    }

                    var msgTelemetry = this.TelemetryFactory.BuildMessageTelemetry(string.Format(message, args), level);
                    this.TelemetryClient.Track(msgTelemetry);
                }
            }
        }

        public override void TrackEvent(string name)
        {            
            if(this.TelemetryFactory != null && this.TelemetryClient != null && !string.IsNullOrEmpty(name))
            {
                this.TelemetryClient.Track(this.TelemetryFactory.BuildEventTelemetry(name));
            }
        }

        public override void TrackException(Exception ex)
        {            
            if(this.TelemetryFactory != null && this.TelemetryClient !=null && ex != null)
            {
                this.TelemetryClient.Track(this.TelemetryFactory.BuildExceptionTelemetry(ex));
            }
        }
    }
}
