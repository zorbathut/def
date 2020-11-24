namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Base class for recordable elements.
    /// </summary>
    /// <remarks>
    /// Inheriting from this is the easiest way to support Recorder serialization.
    ///
    /// If you need to record a class that you can't modify the definition of, see the Converter system.
    /// </remarks>
    public interface IRecordable
    {
        /// <summary>
        /// Serializes or deserializes this object to Recorder.
        /// </summary>
        /// <remarks>
        /// This function is called both for serialization and deserialization. In most cases, you can simply call Recorder.Record functions to do the right thing.
        ///
        /// For more complicated requirements, check out Recorder's interface.
        /// </remarks>
        /// <example>
        /// <code>
        ///     public void Record(Recorder recorder)
        ///     {
        ///         // The Recorder interface figures out the right thing based on context.
        ///         // Any members that are referenced elsewhere will be turned into refs automatically.
        ///         // Members that don't show up in the saved data will be left at their default value.
        ///         recorder.Record(ref integerMember, "integerMember");
        ///         recorder.Record(ref classMember, "classMember");
        ///         recorder.Record(ref structMember, "structMember");
        ///         recorder.Record(ref collectionMember, "collectionMember");
        ///     }
        /// </code>
        /// </example>
        void Record(Recorder recorder);
    }

    /// <summary>
    /// Main class for the serialization/deserialization system.
    /// </summary>
    /// <remarks>
    /// Recorder is used to call the main functions for serialization/deserialization. This includes both the static initiation functions (Read, Write) and the per-element status functions.
    ///
    /// To start serializing or deserializing an object, see Recorder.Read and Recorder.Write.
    /// </remarks>
    public abstract class Recorder
    {
        /// <summary>
        /// Serialize or deserialize a member of a class.
        /// </summary>
        /// <remarks>
        /// This function serializes or deserializes a class member. Call it with a reference to the member and a label for the member (usually the member's name.)
        ///
        /// In most cases, you don't need to do anything different for read vs. write; this function will figure out the details and do the right thing.
        /// </remarks>
        public abstract void Record<T>(ref T value, string label);

        /// <summary>
        /// Indicates whether this Recorder is being used for reading or writing.
        /// </summary>
        public enum Direction
        {
            Read,
            Write,
        }
        /// <summary>
        /// Indicates whether this Recorder is being used for reading or writing.
        /// </summary>
        public abstract Direction Mode { get; }

        /// <summary>
        /// Returns the XML element that is being read or written to.
        /// </summary>
        /// <remarks>
        /// Generally not necessary to use; this is intended for when you're doing hand-written serialization and deserialization work.
        /// </remarks>
        public abstract XElement Xml { get; }

        /// <summary>
        /// Returns a fully-formed XML document starting at an object.
        /// </summary>
        public static string Write<T>(T target, bool pretty = true)
        {
            var writerContext = new WriterXmlRecord();

            Serialization.ComposeElement(writerContext.StartData(), target, target != null ? target.GetType() : typeof(T));

            return writerContext.Finish(pretty);
        }

        /// <summary>
        /// Returns C# validation code starting at an option.
        /// </summary>
        public static string WriteValidation<T>(T target)
        {
            var writerContext = new WriterValidationRecord();

            Serialization.ComposeElement(writerContext.StartData(), target, target != null ? target.GetType() : typeof(T));

            return writerContext.Finish();
        }

        /// <summary>
        /// Parses the output of Write, generating an object and all its related serialized data.
        /// </summary>
        public static T Read<T>(string input, string stringName = "input")
        {
            XDocument doc;

            try
            {
                doc = XDocument.Parse(input, LoadOptions.SetLineInfo);
            }
            catch (System.Xml.XmlException e)
            {
                Dbg.Ex(e);
                return default(T);
            }

            if (doc.Elements().Count() > 1)
            {
                // This isn't testable, unfortunately; XDocument doesn't even support multiple root elements.
                Dbg.Err($"{stringName}: Found {doc.Elements().Count()} root elements instead of the expected 1");
            }

            var record = doc.Elements().First();
            if (record.Name.LocalName != "Record")
            {
                Dbg.Wrn($"{stringName}:{record.LineNumber()}: Found root element with name \"{record.Name.LocalName}\" when it should be \"Record\"");
            }

            var recordFormatVersion = record.ElementNamed("recordFormatVersion");
            if (recordFormatVersion == null)
            {
                Dbg.Err($"{stringName}:{record.LineNumber()}: Missing record format version, assuming the data is up-to-date");
            }
            else if (recordFormatVersion.GetText() != "1")
            {
                Dbg.Err($"{stringName}:{recordFormatVersion.LineNumber()}: Unknown record format version {recordFormatVersion.GetText()}, expected 1 or earlier");

                // I would rather not guess about this
                return default(T);
            }

            var refs = record.ElementNamed("refs");

            var readerContext = new ReaderContext(stringName, true);

            if (refs != null)
            {
                // First, we need to make the instances for all the references, so they can be crosslinked appropriately
                foreach (var reference in refs.Elements())
                {
                    if (reference.Name.LocalName != "Ref")
                    {
                        Dbg.Wrn($"{stringName}:{reference.LineNumber()}: Reference element should be named 'Ref'");
                    }

                    var id = reference.Attribute("id")?.Value;
                    if (id == null)
                    {
                        Dbg.Err($"{stringName}:{reference.LineNumber()}: Missing reference ID");
                        continue;
                    }

                    var className = reference.Attribute("class")?.Value;
                    if (className == null)
                    {
                        Dbg.Err($"{stringName}:{reference.LineNumber()}: Missing reference class name");
                        continue;
                    }

                    var possibleType = (Type)Serialization.ParseString(className, typeof(Type), null, stringName, reference.LineNumber());
                    if (possibleType.IsValueType)
                    {
                        Dbg.Err($"{stringName}:{reference.LineNumber()}: Reference assigned type {possibleType}, which is a value type");
                        continue;
                    }

                    // Create a stub so other things can reference it later
                    readerContext.refs[id] = Activator.CreateInstance(possibleType);
                    if (readerContext.refs[id] == null)
                    {
                        // This is difficult to test; there are very few things that can get CreateInstance to return null, and right now the type system doesn't support them (int? for example)
                        Dbg.Err($"{stringName}:{reference.LineNumber()}: Reference of type {possibleType} was not properly created; this will cause issues");
                        continue;
                    }
                }

                // Now that all the refs exist, we can run through them again and actually parse them
                foreach (var reference in refs.Elements())
                {
                    var id = reference.Attribute("id")?.Value;
                    if (id == null)
                    {
                        // Just skip it, we don't have anything useful we can do here
                        continue;
                    }

                    // The serialization routines don't know how to deal with this, so we'll remove it now
                    reference.Attribute("id").Remove();

                    var refInstance = readerContext.refs.TryGetValue(id);
                    if (refInstance == null)
                    {
                        // We failed to parse this for some reason, so just skip it now
                        continue;
                    }

                    // Do our actual parsing
                    var refInstanceOutput = Serialization.ParseElement(reference, refInstance.GetType(), refInstance, readerContext, hasReferenceId: true);

                    if (refInstance != refInstanceOutput)
                    {
                        Dbg.Err($"{stringName}:{reference.LineNumber()}: Something really bizarre has happened and we got the wrong object back. Things are probably irrevocably broken. Please report this as a bug in Def.");
                        continue;
                    }
                }
            }

            var data = record.ElementNamed("data");
            if (data == null)
            {
                Dbg.Err($"{stringName}:{record.LineNumber()}: No data element provided. This is not very recoverable.");

                return default(T);
            }

            // And now, we can finally parse our actual root element!
            // (which accounts for a tiny percentage of things that need to be parsed)
            return (T)Serialization.ParseElement(data, typeof(T), null, readerContext);
        }
    }

    internal class RecorderWriter : Recorder
    {
        private readonly HashSet<string> fields = new HashSet<string>();
        private readonly WriterNode node;

        internal RecorderWriter(WriterNode node)
        {
            this.node = node;
        }

        public override void Record<T>(ref T value, string label)
        {
            if (fields.Contains(label))
            {
                Dbg.Err($"Field '{label}' written multiple times");
                return;
            }

            fields.Add(label);

            Serialization.ComposeElement(node.CreateChild(label), value, typeof(T));
        }

        public override XElement Xml { get => node.GetXElement(); }
        public override Direction Mode { get => Direction.Write; }
    }

    internal class ReaderContext
    {
        public string sourceName;
        public Dictionary<string, object> refs;

        public bool RecorderMode { get => refs != null; }

        public ReaderContext(string sourceName, bool withRefs)
        {
            this.sourceName = sourceName;

            if (withRefs)
            {
                refs = new Dictionary<string, object>();
            }
        }
    }

    internal class RecorderReader : Recorder
    {
        private readonly XElement element;
        private readonly ReaderContext context;

        public string SourceName { get => context.sourceName; }

        internal RecorderReader(XElement element, ReaderContext context)
        {
            this.element = element;
            this.context = context;
        }

        public override void Record<T>(ref T value, string label)
        {
            var recorded = element.ElementNamed(label);
            if (recorded == null)
            {
                return;
            }

            // Explicit cast here because we want an error if we have the wrong type!
            value = (T)Serialization.ParseElement(recorded, typeof(T), value, context);
        }

        public override XElement Xml { get => element; }
        public override Direction Mode { get => Direction.Read; }
    }
}
