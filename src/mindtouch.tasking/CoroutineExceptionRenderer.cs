using System;
using System.IO;
using log4net.ObjectRenderer;

namespace MindTouch.Tasking {
    internal class CoroutineExceptionRenderer : IObjectRenderer {

        //--- Methods ---
        public void RenderObject(RendererMap rendererMap, object obj, TextWriter writer) {
            if(obj is Exception) {
                writer.Write(((Exception)obj).GetCoroutineStackTrace());
            }
        }
    }
}