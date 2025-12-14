import db from "../config/db.js";

export default class Event {
  constructor(event_id, venue_name, event_artist, event_date, event_price) {
    this.event_id = event_id;
    this.venue_name = venue_name;
    this.event_artist = event_artist;
    this.event_date = event_date;
    this.event_price = event_price;
  }

  static findAll() {
    return db.execute("SELECT * FROM view_events");
  }
}
