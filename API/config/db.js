import dotenv from "dotenv";
import mysql from "mysql2/promise";

dotenv.config();

const pool = mysql.createPool({
  host: process.env.GIGS_DB_SERVER,
  user: process.env.GIGS_DB_USER,
  database: process.env.GIGS_DB_NAME,
  password: process.env.GIGS_DB_PASSWORD,
});

export default pool;
