// SimpleWebRTC signaling relay.
// Relays every message to all other connected clients. Peers filter by room and receiver themselves,
// so the server does not need to understand the SimpleWebRTC protocol.
// Usage: npm install && npm start   (PORT environment variable, default 3000)

const http = require("http");
const { WebSocketServer, WebSocket } = require("ws");

const port = process.env.PORT || 3000;

const server = http.createServer((req, res) => {
    res.writeHead(200, { "Content-Type": "text/plain" });
    res.end("SimpleWebRTC signaling server running!");
});

const wss = new WebSocketServer({ server });

wss.on("connection", (socket, request) => {
    console.log(`Client connected (${wss.clients.size} total) from ${request.socket.remoteAddress}`);

    socket.on("message", (data, isBinary) => {
        for (const client of wss.clients) {
            if (client !== socket && client.readyState === WebSocket.OPEN) {
                client.send(data, { binary: isBinary });
            }
        }
    });

    socket.on("close", () => {
        console.log(`Client disconnected (${wss.clients.size} total)`);
    });
});

server.listen(port, () => {
    console.log(`SimpleWebRTC signaling server listening on ws://localhost:${port}`);
});
