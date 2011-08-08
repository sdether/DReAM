using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MindTouch.Traum.Webclient.Test {

    [TestFixture]
    public class AsyncStreamCopierTests {

        [Test]
        public void Can_copy_memorystream_to_memorystream() {
            var input = new MemoryStream();
            var writer = new StreamWriter(input);
            var data = StringUtil.CreateAlphaNumericKey(41 * 1024);
            writer.Write(data);
            input.Position = 0;
            var output = new MemoryStream();
            AsyncStreamCopier.Copy(input, output, (int)input.Length).Wait();
            output.Position = 0;
            var reader = new StreamReader(output);
            Assert.AreEqual(data, reader.ReadToEnd());
        }
    }
}
