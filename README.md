# SharpFont

Implements a parser and renderer for TTF / OTF font files.

## Changes for FUSEE FontImp.WebAsm

- Made several methods public
- Implemented some missing methods (metrics, etc.)
- Deleted the GPU example to prevent any dependencies to windows.forms
- Deleted tests and unnecessary stuff for  a really clean netstandard 2.0 lib