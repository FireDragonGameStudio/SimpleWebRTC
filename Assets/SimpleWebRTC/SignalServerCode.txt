const express = require("express");
const app = express();
const http = require("http");
const server = http.createServer(app);

server.listen(3000);

app.get("/", (req, res) => res.send('Signal Server Running!'));

const webSocket = require("ws");
const wss = new webSocket.Server({ server });

wss.on("connection", function (socket) {
    // Some feedback on the console
    console.log("A client just connected");

    socket.on("message", function (msg) {
        console.log("Received message from client: " + msg);
        
        // Broadcast that message to all connected clients except sender
        wss.clients.forEach(function (client) {
          console.log("Send to client: " + client + ": " + (client !== socket));
          
          if (client !== socket) {
            client.send(msg);
          }
        });
    });

    socket.on("close", function () {
        console.log("Client disconnected");
    });
});