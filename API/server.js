import express from "express";
import mysql from "mysql2";
import cors from "cors";
import dotenv from "dotenv";
import eventRoutes from "./routes/eventRoutes.js";

const app = express();
const port = process.env.PORT;

// Middleware
app.use(cors());
app.use(express.json());

// Routes
app.get("/", (req, res) => {
  res.json({
    status: "ok",
    message: "GigCalendar API",
    timestamp: new Date().toISOString(),
  });
});

app.get("/health", (req, res) => {
  res.status(200).send("OK");
});

app.use("/api/events", eventRoutes);

// Global Error Handler. IMPORTANT function params MUST start with err
app.use((err, req, res, next) => {
  console.log(err.stack);
  console.log(err.name);
  console.log(err.code);

  res.status(500).json({
    message: "Something went wrong:",
  });
});

app.listen(port, () => {
  console.log(`Server listening on http://[server]:${port}`);
  console.log(`Health check: http://[server]:${port}/health`);
  console.log(`Events endpoint: http://[server]:${port}/api/events`);
});
