/********************************************************************************
* TaskExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.Proxy.Internals
{
    internal static class TaskExtensions
    {
        public static Task<T> AsCancellable<T>(this Task<T> task, in CancellationToken cancellation)
        {
            TaskCompletionSource<T> tcs = new();

            //
            // If the cancellation already requested then the SetCanceled() gets called immediately.
            //

            cancellation.Register(tcs.SetCanceled);

            return Task.WhenAny
            (
                task,
                tcs.Task
            ).Unwrap();
        }
    }
}
