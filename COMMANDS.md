# In game console

F11 opens it, Enter sends. No slash in front: the client keeps anything that starts with `/`
for itself. Everything is typed straight, `admin online`, not `/admin online`.

`help` lists what you are allowed to run, one command per line.

## Everyone

| Command | What it does |
|---|---|
| `help` | the list below, filtered by your security level |

## Game master

| Command | What it does |
|---|---|
| `server status` | uptime, players online and the peak since the server came up |
| `reload shop` | reads the shop tables again without a restart |
| `game state <state>` | forces the state of the room you are in |
| `inventory list` | your items with their ids |
| `inventory showitem <id>` | what one of your items is |
| `inventory set <...>` | edits one of your items |
| `gm kick <nickname>` | throws him back to the server list |
| `gm ban <nickname>` | kicks and blocks the account |
| `gm killroom` | ends the room you are in |
| `admin online` | how many are connected and who they are |
| `admin where <nickname>` | his channel, his room, the mode, the state of the match and his level |
| `admin notice <message>` | the yellow line across everybody's screen |
| `admin pen <nickname> <amount>` | sets his PEN and refreshes his window |
| `admin ap <nickname> <amount>` | the same with AP |
| `admin level <nickname> <level>` | sets the level and the experience that belongs to it |
| `admin endmatch <room id>` | sends a match that is playing to the result screen |

## Developer

| Command | What it does |
|---|---|
| `admin seclevel <nickname> <0 user, 1 gm, 2 developer>` | until he logs out, the security level itself lives in the auth database |

The room id for `endmatch` is the one `admin where` prints.
