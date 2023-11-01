/********************************************************************************
* IsExternalInit.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

//
// Init accessor requires IsExternalInit class to be defined (which is not provided by the runtime before NET5_0)
// https://stackoverflow.com/questions/62648189/testing-c-sharp-9-0-in-vs2019-cs0518-isexternalinit-is-not-defined-or-imported
// We have to define it for all the TFMs.:
// https://twitter.com/aarnott/status/1362786409954766858
//

namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }
}
