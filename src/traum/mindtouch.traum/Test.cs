using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MindTouch.Traum {
    class Test {

        public async Task<int> X() {
            return await TaskEx.Run(() => 1);
        }

        public Task<int> Y() {
            return TaskEx.FromResult<int>(0);
        }

        public async Task T() {
            var x = await X();
            var y = await Y();
        }

        public async void btnDownload_Click(object sender, EventArgs e) {
            Task<int> download = TaskEx.FromResult<int>(0);
            if(download == await TaskEx.WhenAny(download, TaskEx.Delay(3000))) {
                var bmp = await download;
            } else {
                download.ContinueWith(t => Console.WriteLine("finally done"));
            }
        }

    }
}
