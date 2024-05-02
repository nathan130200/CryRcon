### Cry Rcon
Connect to remote cryengine dedicated server using crytek rcon api.

### Usage
1. Start cryengine dedicated server with param `+rcon_startserver "port:1234 pass:youshallnotpass" `
2. Start rcon client with same params `./Rcon --host=yourserverip --port=sameportused --password=samepassword`
3. Wait untill connect and start sending remote commands to your server.

### Example

<img src="https://i.imgur.com/jhJybQJ.png" alt="Cry Rcon working with some cryengine server">


#### Notes

- By default cryengine dedicated server don't trim color codes $[0-9] so i decided to implement a basic parser for colorized output in rcon client.
- Rcon client uses TCP while game server uses UDP. You can share the same port in rcon client and the game while testing your server/network code (not recommended).