# Installation

The client is implemented as a Windows service. The service uses the SysInternals [Sysmon](https://technet.microsoft.com/en-gb/sysinternals/sysmon) tool to extract the system activity. The Sysmon application needs to be downloaded and installed on the host before the client service is installed.

The service uses the configuration details within the **SysMonLogger.xml** file. The file contains two settings:

- CertificateFileName: The file name of the servers TLS certificate
- RemoteServer: The IP/hostName and port of the remote analysis server e.g. **192.168.0.100:8000**

The service uses the analysis servers TLS certificate file (server.pem) to perform certificate pinning. The file should be copied into the same directory as the rest of the AutoRuns client files.

The AutoRuns client files need to be copied to the target host. The required files are:

```
SysMonLogger.exe
SysMonLogger.xml
server.pem
```
Once the files have been copied to the host, the file permissions should be modified to prevent other users from modifying the files.

## Service

The **SysMonLogger.exe** has automatic Windows service installation code which means that the use of **sc.exe** or **InstallUtil.exe** is not required.

To install the service, use the following command from an elevated command prompt:
```
SysMonLogger.exe -install
```

To uninstall the service, use the following command from an elevated command prompt:
```
SysMonLogger.exe -uninstall
```

## Sysmon Filtering

Minor changes to the Sysmon configuration can be performed by the command line, however, the majority is performed by supplying a configuration (XML) file to the Sysmon service. It is important that the Sysmon configuration is bespoke to the host and environment that Sysmon is installed. If the configuration is poorly implemented it can cause a performance issue and will generate too much logging data, which in turn will reduce the effectiveness of the system.

The events that are available for monitoring with Sysmon v4 are:

- Process Creation
- Process Changed File Creation Time
- Network Connection
- Sysmon Service State Changed
- Process Terminate
- Driver Loaded
- Image Loaded
- Create Remote Thread
- Raw Access Read
- Error

An example of where filtering is required would be when software disk encryption is used, which can generate numerous amounts of **Raw Access Read** events. Another example on a server installation is where a server application creates numerous network connections, therefore the application could be excluded from the **Network Connection** event logging.

The key events to monitor are **Process Creation**, **Process Terminate**, **Network Connection**, **Driver Loaded** and **Create Remote Thread**. The **Image Loaded** and **Raw Access Read** events should be used with caution due to the potential event volume. The **Error** and **Sysmon Service State Changed** events are more to do with operational events.
