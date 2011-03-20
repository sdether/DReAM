using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MindTouch.Dream;
using MindTouch.Xml;
using NUnit.Framework;

namespace MindTouch.Web.Client.Test {
    
    [TestFixture]
    public class PhpUtilTests {
        [Test]
        public void PhpSerialization1() {
            XDoc doc = new XDoc("test");
            doc.Value("<tag>\"text\"</tag>");
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            MemoryStream stream = new MemoryStream();
            PhpUtil.WritePhp(doc, stream, encoding);
            byte[] text = stream.ToArray();
            Assert.IsTrue(ArrayUtil.Compare(encoding.GetBytes("a:1:{s:4:\"test\";s:17:\"<tag>\"text\"</tag>\";}"), text) == 0);
        }

        [Test]
        public void PhpSerialization2() {
            XDoc doc = new XDoc("test");
            doc.Value("<tag>text\ntext</tag>");
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            MemoryStream stream = new MemoryStream();
            PhpUtil.WritePhp(doc, stream, encoding);
            byte[] text = stream.ToArray();
            Assert.IsTrue(ArrayUtil.Compare(encoding.GetBytes("a:1:{s:4:\"test\";s:20:\"<tag>text\ntext</tag>\";}"), text) == 0);
        }

        [Test]
        public void PhpSerialization3() {
            XDoc doc = new XDoc("test");
            doc.Value("<tag>ö</tag>");
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            MemoryStream stream = new MemoryStream();
            PhpUtil.WritePhp(doc, stream, encoding);
            byte[] text = stream.ToArray();
            Assert.IsTrue(ArrayUtil.Compare(encoding.GetBytes("a:1:{s:4:\"test\";s:12:\"<tag>ö</tag>\";}"), text) == 0);
        }
    }
}
