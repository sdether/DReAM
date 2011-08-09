using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using log4net;

namespace MindTouch.Traum.Webclient.Test {

    [TestFixture]
    public class AsyncStreamCopierTests {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        [Test]
        public void Can_copy_memorystream_to_memorystream() {
            var input = new MemoryStream();
            var writer = new StreamWriter(input);
            var data = StringUtil.CreateAlphaNumericKey(41 * 1024);
            writer.Write(data);
            writer.Flush();
            input.Position = 0;
            var output = new MemoryStream();
            _log.Debug("async copy of memorystream to memorystream");
            AsyncStreamCopier.Copy(input, output, (int)input.Length).Wait();
            output.Position = 0;
            var reader = new StreamReader(output);
            Assert.AreEqual(data, reader.ReadToEnd());
        }

        [Test]
        public void Can_copy_file_to_memorystream() {
            var file = Path.GetTempFileName();
            try {
                var data = StringUtil.CreateAlphaNumericKey(41 * 1024);
                File.WriteAllText(file, data);
                var input = File.OpenRead(file);
                var output = new MemoryStream();
                _log.Debug("async copy of file to memorystream");
                AsyncStreamCopier.Copy(input, output, (int)input.Length).Wait();
                output.Position = 0;
                var reader = new StreamReader(output);
                Assert.AreEqual(data, reader.ReadToEnd());
            } finally {
                try {
                    File.Delete(file);
                } catch { }
            }
        }

        [Test]
        public void Can_copy_memorystream_to_file() {
            var file = Path.GetTempFileName();
            try {
                var input = new MemoryStream();
                var writer = new StreamWriter(input);
                var data = StringUtil.CreateAlphaNumericKey(41 * 1024);
                writer.Write(data);
                writer.Flush();
                input.Position = 0;
                var output = File.OpenWrite(file);
                _log.Debug("async copy of file to memorystream");
                AsyncStreamCopier.Copy(input, output, (int)input.Length).Wait();
                output.Close();
                var data2 = File.ReadAllText(file);
                Assert.AreEqual(data, data2);
            } finally {
                try {
                    File.Delete(file);
                } catch { }
            }
        }

        [Test]
        public void Can_copy_file_to_file() {
            var inputFile = Path.GetTempFileName();
            var outputFile = Path.GetTempFileName();
            try {
                var data = StringUtil.CreateAlphaNumericKey(41 * 1024);
                File.WriteAllText(inputFile, data);
                var input = File.OpenRead(inputFile);
                var output = File.OpenWrite(outputFile);
                _log.Debug("async copy of file to memorystream");
                AsyncStreamCopier.Copy(input, output, (int)input.Length).Wait();
                output.Close();
                var data2 = File.ReadAllText(outputFile);
                Assert.AreEqual(data, data2);
            } finally {
                try {
                    File.Delete(inputFile);
                    File.Delete(outputFile);
                } catch { }
            }
        }
    }
}
