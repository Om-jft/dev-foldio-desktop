using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace CameraControl.DSLRPCToolSub.Classes
{
    public static class AsyncOperationExtensions
    {
        public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> operation)
        {
            var tcs = new TaskCompletionSource<TResult>();

            operation.Completed += delegate
            {
                switch (operation.Status)
                {
                    case AsyncStatus.Completed:
                        tcs.TrySetResult(operation.GetResults());
                        break;

                    case AsyncStatus.Error:
                        tcs.TrySetException(operation.ErrorCode);
                        break;

                    case AsyncStatus.Canceled:
                        tcs.SetCanceled();
                        break;

                }
            };

            return tcs.Task;
        }
    }
}
