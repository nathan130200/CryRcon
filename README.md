### Cry Rcon
Connect to remote cryengine dedicated server using crytek rcon api.

### Usage
1. Start cryengine dedicated server with param `+rcon_startserver "port:1234 pass:youshallnotpass" `
2. Start rcon client with same params `./Rcon --host=yourserverip --port=sameportused --password=samepassword`
3. Wait untill connect and start sending remote commands to your server.

### Example

<img src="https://i.imgur.com/jhJybQJ.png" alt="Cry Rcon working with some cryengine server">

> By default cryengine dedicated server don't trim color codes $[0-9] so i decided to implement a basic parser for colorized output in rcon client.