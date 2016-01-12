# AlarmSystem
A universal alarm handling system completely based on plugins.

*This project is distributed under MIT License*

## What is it?
AlarmSystem is a system mainly created for fire fighters or ambulances to use in their houses. It's first task was to handle alarms received from anywhere (for example a pager) and pass information about the event to all the members of the organisation using SMS and Telegram.
It's second task was to print SMS messages of members, telling when they arrive at the house to that a dispatcher can better dispatch the resources available.

Well. It has grown since then and is now a fully modular software you can easily extend with plugins. There are some examples of those plugins included in this repository. Please note that there will be a homepage soon for documenting all needed tasks during developement and usage.

## Included Plugins:
 - SMS Plugin - To handle incoming and outgoing SMS
 - Printer Plugin - To Print messages and freetext on a ESC/POS bill printer
 - Telegram Plugin - Handles all telegram communications
 - HTTP Server Plugin - Receives Trigger Requests over HTTP
 - Trigger Request Handler - Transforms trigger requests into Messages and Freetext to be shown or printed

The main application by now only offers a configuration service and a database connection to all plugins.

## Configuration
See AlarmSystemConfig.example.xml - Documentation will follow soon. This file needs to be renamed to AlarmSystemConfig.xml before compiling and running.

## Dependencies
There are some dependencies:
 - For SMS, TriggerRequestHandler to work you need the https://github.com/2steuer/alarm2sms-web Web Interface (based on laravel) installed and configured. Note the Database Seed which adds admin@admin.com:admin as default credentials.
   Further instructions on this will be made public in near future.
 - For Telegram Plugin, please download the source of https://github.com/MrRoundRobin/telegram.bot 
   When done, remove all Dependencies but NewtonSoft.JSON and WebApi.Client and compile it from source. Thenn add the reference to Telegram Plugin manually.
 - For Printer Plugin: https://github.com/yukimizake/ThermalDotNet Compile from source and add the reference to the project
 - For Sms Plugin: 
    - https://github.com/2steuer/GSMComm Compile from source and add GSMCommShared and GSMCommunication to the SMS Project.
    - http://www.scampers.org/steve/sms/libraries.htm Download GSMComm from here and add PDUConverter to the project references

Sadly, the steps with GSMComm are needed to ensure the compatibility to mono (since some SerialPort issues), but the PDUConverter from the repository is broken. So we need the original, closed-source and pre-compiled library from here.
