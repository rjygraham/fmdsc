# File Metadata Sidecar (fmdsc)

The File Metadata Sidecar is useful in situations where file attributes need to be maintained when copying files across storage systems that do not persist (or support) file attributes.

File Metadata Sidecar works by scanning the supplied path for all content files and creating a .meta file alongside the content file. Once the content files have reached their destination or on a storage system that supports file attributes, run fmdsc again to restore the file attributes from the .meta file and optionally delete the .meta file.

---
Development: ![.NET Core](https://github.com/rjygraham/fmdsc/workflows/.NET%20Core/badge.svg)

---

## Usage

To use fmdsc, you need to specify either the `create` or `restore` verbs depending on which operation you're trying to run.

````
C:>fmdsc help
fmdsc 0.1.0
Copyright (C) 2020 fmdsc

create     Create file attributes metadata files.

restore    Restore file attributes.

help       Display more information on a specific command.

version    Display version information.
````

### Create

To create the metadata files, you'll execute `fmdsc create` and specify the path with the `-p` option.

````
C:>fmdsc create --help
fmdsc 0.1.0
Copyright (C) 2020 fmdsc

-p, --path    Required. Set the input folder to scan.

--help        Display this help screen.

--version     Display version information.
````

For example, to create metadata files for all files (recursively) beneath `C:\example` you would execute the following command:

````
C:>fmdsc create -p "C:\example"
````

### Restore

To restore file attributes, you'll execute `fmdsc restore` and specify the path with the `-p` option. You can also optionally provide the `-d` option to delete the metadata files.

````
C:>fmdsc restore help
fmdsc 0.1.0
Copyright (C) 2020 fmdsc

ERROR(S):
Required option 'p, path' is missing.

-p, --path      Required. Set the input folder to scan.

-d, --delete    Delete the source metadata file.

--help          Display this help screen.

--version       Display version information.
````

For example, to restore the file attributes for all files (recursively) beneath `C:\example` you would execute the following command:

````
C:>fmdsc restore -p "C:\example"
````

And if you wanted to delete the source metadata files, you would exeucte the following command:

````
C:>fmdsc restore -d -p "C:\example"
````

### Logging

fmdsc only logs to console output. If you need to record progress in a log file, pipe output to your file of choice. For example:

````
C:>fmdsc create -p "C:\example" > "C:\logs\fmdsc-create.log"
C:>fmdsc restore -p "C:\example" > "C:\logs\fmdsc-restore.log"
````

---

## License

Copyright 2020 Ryan Graham

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.