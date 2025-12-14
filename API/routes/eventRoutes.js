import express from "express";
import controller from "../controllers/eventController.js";

const eventRoutes = express.Router();

eventRoutes.route("/").get(controller.getEvents);

export default eventRoutes;
