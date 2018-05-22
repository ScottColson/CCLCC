﻿using CCLLC.Telemetry.Implementation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;


namespace CCLLC.Telemetry.Serializer
{
    public class TelemetrySerializer : ITelemetrySerializer
    {
        private IContextTagKeys contextTagKeys;
        private readonly UTF8Encoding transmissionEncoding = new UTF8Encoding(false);
        public virtual UTF8Encoding TransmissionEncoding { get { return this.transmissionEncoding; } }

        /// <summary>
        /// Gets the compression type used by the serializer. 
        /// </summary>
        public virtual string CompressionType
        {
            get
            {
                return "gzip";
            }
        }

        /// <summary>
        /// Gets the content type used by the serializer. 
        /// </summary>
        public virtual string ContentType
        {
            get
            {
                return "application/x-json-stream";
            }
        }

        public TelemetrySerializer(IContextTagKeys contextTagKeys)
        {
            if (contextTagKeys == null) { throw new ArgumentNullException("contextTagKeys"); }
            this.contextTagKeys = contextTagKeys;
        }

        /// <summary>
        /// Serializes and compress the telemetry items into an array of JSON objects. 
        /// </summary>
        /// <param name="telemetryItems">The list of telemetry items to serialize.</param>
        /// <param name="compress">Should serialization also perform compression.</param>
        /// <returns>The compressed and serialized telemetry items.</returns>       
        public virtual byte[] Serialize(IEnumerable<ITelemetry> telemetryItems, bool compress = true)
        {
            var memoryStream = new MemoryStream();
            using (Stream stream = compress ? CreateCompressedStream(memoryStream) : memoryStream)
            {
                using (var streamWriter = new StreamWriter(stream, TransmissionEncoding))
                {
                    SerializeToStream(telemetryItems, streamWriter);
                }
            }

            return memoryStream.ToArray();
        }

        /// <summary>
        /// Serializes <paramref name="telemetryItems"/> into an array and write the response to <paramref name="streamWriter"/>.
        /// </summary>
        protected virtual void SerializeToStream(IEnumerable<ITelemetry> telemetryItems, TextWriter streamWriter)
        {
            JsonWriter jsonWriter = new JsonWriter(streamWriter);

            int telemetryCount = 0;
            jsonWriter.WriteStartArray();

            foreach (var telemetryItem in telemetryItems)
            {
                if (telemetryCount++ > 0)
                {
                    streamWriter.Write(",");                    
                }

                telemetryItem.Sanitize();
                Serialize(telemetryItem, jsonWriter);
            }

            jsonWriter.WriteEndArray();
        }


        protected virtual void Serialize(ITelemetry item, IJsonWriter writer)
        {
            writer.WriteStartObject();

            WriteTelemetryName(item, writer);
            WriteEnvelopeProperties(item, writer, contextTagKeys);
            writer.WritePropertyName("data");
            {
                writer.WriteStartObject();

                //serialize the main data tags for the telemetry.
                var dataTags = item.GetTaggedData();
                foreach(var tag in dataTags)
                {
                    if(!string.IsNullOrEmpty(tag.Key) && !string.IsNullOrEmpty(tag.Value))
                    {
                        writer.WriteProperty(tag.Key, tag.Value);
                    }
                }

                //if the telemetry has additional properties then serialize the properties into a
                //list of name/value pairs.
                var withProperties = item as ISupportProperties;
                if(withProperties != null && withProperties.Properties.Count > 0)
                {
                    writer.WriteProperty("properties", withProperties.Properties);
                }

                //if the telemetry has measurements then serialize the measurements into a
                //list of name/value pairs.
                var withMeasurements = item as ISupportMetrics;
                if(withMeasurements != null && withMeasurements.Metrics.Count > 0)
                {
                    writer.WriteProperty("measurements", withMeasurements.Metrics);
                }

                //if the telemtry has a list of exception details then serialize the list
                //into a JSON array.
                var withExceptions = item as IExceptionTelemetry;
                if(withExceptions != null && withExceptions.ExceptionDetails.Count > 0)
                {
                    writer.WritePropertyName("exceptions");
                    {
                        writer.WriteStartArray();

                        SerializeExceptions(withExceptions.ExceptionDetails, writer);

                        writer.WriteEndArray();
                    }                    
                }
                
                writer.WriteEndObject();
            }

            writer.WriteEndObject();

        }
        
        /// <summary>
        /// Creates a GZIP compression stream that wraps <paramref name="stream"/>. For windows phone 8.0 it returns <paramref name="stream"/>. 
        /// </summary>
        protected virtual Stream CreateCompressedStream(Stream stream)
        {
            return new GZipStream(stream, CompressionMode.Compress);
        }

        protected virtual void WriteTelemetryName(ITelemetry telemetry, IJsonWriter json)
        {            
            json.WriteProperty("type", telemetry.TelemetryName);
        }

        protected virtual void WriteEnvelopeProperties(ITelemetry telemetry, IJsonWriter json, IContextTagKeys keys)
        {
            json.WriteProperty("time", telemetry.Timestamp.UtcDateTime.ToString("o", CultureInfo.InvariantCulture));

            var samplingSupportingTelemetry = telemetry as ISupportSampling;

            if (samplingSupportingTelemetry != null
                && samplingSupportingTelemetry.SamplingPercentage.HasValue
                && (samplingSupportingTelemetry.SamplingPercentage.Value > 0.0 + 1.0E-12)
                && (samplingSupportingTelemetry.SamplingPercentage.Value < 100.0 - 1.0E-12))
            {
                json.WriteProperty("sampleRate", samplingSupportingTelemetry.SamplingPercentage.Value);
            }

            json.WriteProperty("seq", telemetry.Sequence);
            WriteTelemetryContext(json, telemetry.Context, keys);
        }

        protected virtual void WriteTelemetryContext(IJsonWriter json, ITelemetryContext context, IContextTagKeys keys)
        {
            if (context != null)
            {                
                json.WriteProperty("context", context.ToContextTags(keys));
            }
        }        

        public virtual void SerializeExceptions(IEnumerable<IExceptionDetails> exceptions, IJsonWriter writer)
        {
            int exceptionArrayIndex = 0;

            foreach (IExceptionDetails exceptionDetails in exceptions)
            {
                if (exceptionArrayIndex++ != 0)
                {
                    writer.WriteComma();
                }

                writer.WriteStartObject();
                writer.WriteProperty("id", exceptionDetails.id);
                if (exceptionDetails.outerId != 0)
                {
                    writer.WriteProperty("outerId", exceptionDetails.outerId);
                }

                writer.WriteProperty(
                    "typeName",
                    Utils.PopulateRequiredStringValue(exceptionDetails.typeName));
                writer.WriteProperty(
                    "message",
                    Utils.PopulateRequiredStringValue(exceptionDetails.message));

                if (exceptionDetails.hasFullStack)
                {
                    writer.WriteProperty("hasFullStack", exceptionDetails.hasFullStack);
                }

                writer.WriteProperty("stack", exceptionDetails.stack);

                if (exceptionDetails.parsedStack.Count > 0)
                {
                    writer.WritePropertyName("parsedStack");

                    writer.WriteStartArray();

                    int stackFrameArrayIndex = 0;

                    foreach (IStackFrame frame in exceptionDetails.parsedStack)
                    {
                        if (stackFrameArrayIndex++ != 0)
                        {
                            writer.WriteComma();
                        }

                        writer.WriteStartObject();
                        SerializeStackFrame(frame, writer);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }
        }

        protected virtual void SerializeStackFrame(IStackFrame frame, IJsonWriter writer)
        {
            writer.WriteProperty("level", frame.level);
            writer.WriteProperty(
                "method",
                Utils.PopulateRequiredStringValue(frame.method));
            writer.WriteProperty("assembly", frame.assembly);
            writer.WriteProperty("fileName", frame.fileName);

            // 0 means it is unavailable
            if (frame.line != 0)
            {
                writer.WriteProperty("line", frame.line);
            }
        }
    }

}
