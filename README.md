# ShellyServer
Obligatory disclaimer: Use at your own risk, no warranty is implied or provided and no responsibility taken for any damage caused by using this application.

This small program aims to prevent the boiler struggling to circulate or wasting energy when the valve positions of the TRVs are too low. The logic is simply that it runs a timer with a 5 minute interval, and if the sum total valve positions requested in that time add up to >= 100 it turns on the switch, otherwise it turns it off i.e this can be one radiator fully on, or 2 radiators 50% on etc (This timer relies on the fact the TRVs are designed to access the Valve On URL every 5 minutes when the valve pos is > 0)

To use it you need to:
• Customise the config.json
serverip is the ip4 address of the computer which runs this application (must run as admin)
switchip is the ip4 address of the shelly switch which controls the central heating boiler
• Allow the program through windows firewall and open incoming port 8081 UDP and TCP
• Configure the I/O Action settings for each TRV:
Valve open: http://[serverip]:8081/wcf/trv/[last segment of trv ip address]
eg http://192.168.0.20:8081/wcf/trv/186 
Valve closed: http://[serverip]:8081/wcf/trvoff/[last segment of trv ip address]
eg http://192.168.0.20:8081/wcf/trvoff/186
NB: Assuming you have set up your TRVs according to the Shelly guide, your Valve closed URL will already be set to turn the boiler switch off and you should leave this as a secondary URL for added redundancy (but there should only be one URL for valve on)


















