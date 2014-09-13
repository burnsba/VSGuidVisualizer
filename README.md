VSGuidVisualizer
================

VisualStudio debug visualer plugin to resolve Guids to human friendly identifier


Visualizer plugin the for the VisualStudio debugger. While debugging, mouse over a Guid and click the
magnifying glass. The database will be queried for users and groups; the results are then cached.
If the guid matches one of the results, that is displayed as the resolved name.

Compile and place the DLL in

    C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Packages\Debugger\Visualizers
