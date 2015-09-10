# ShpToSqlServer

A tool used for importing ESRI shapefiles to MS SQL Server

Here's a little tool for you that I've been using quite often lately. It can be used used to import Esri shapefiles into Microsoft SQL Server (2008 and 2012) while taking into account character encoding of the input data. The geospatial information is stored as SqlGeometry.

Nice thing about it is that it allows you to rename/remap destination table column names, choose the destination table name, and set the primary key for the table. And yeah, it has a user interface :)

The app is rather simple so I don't think that further explanation about its usage is needed. If it turns out that I've been wrong, don't hesitate to leave a comment.

I would  like to point out one thing though. If you are not sure which encoding codepage to use, check this [link](http://msdn.microsoft.com/en-us/library/windows/desktop/dd317756(v=vs.85).aspx) and look it up. The codepage default is 65001, which is the code page for UTF-8 encoding.
If you have any suggestions about improving the tool, or find a bug or something, leave a comment, I will be glad to update it.
