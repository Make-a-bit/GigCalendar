import Event from "../models/event.js";

const getEvents = async (req, res, next) => {
  try {
    const [events, _] = await Event.findAll();

    if (events.length > 0) {
      res.status(200).json(events);
    } else {
      res.status(404).json({ message: "No events found" });
    }
  } catch (error) {
    next(error);
  }
};

export default { getEvents };
