import React, { useEffect, useState } from "react";
import { Box, FormControl, InputLabel, MenuItem, Paper, Select } from "@mui/material";
import Event from "../components/Event.jsx";
import MonthComponent from "../components/MonthComponent.jsx";
import theme from "../utils/theme.js";

// Function to fetch data from the API
const fetchData = async () => {
  try {
    const response = await fetch("/api/events");
    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Error fetching data:", error);
  }
};

const Home = () => {
  const [events, setEvents] = useState([]);
  const [selectedVenue, setSelectedVenue] = useState("all");

  // Fetch data on component mount
  useEffect(() => {
    const loadEvents = async () => {
      const data = await fetchData();
      setEvents(data || []);
    };
    loadEvents();
  }, []);

  // Filter events based on selected venue
  const filteredEvents =
    selectedVenue === "all" ? events : events.filter((event) => event.venue_name === selectedVenue);

  // Group events by month
  const groupEventsByMonth = (events) => {
    const grouped = {};

    // Iterate through events and group them by month-year
    events.forEach((event) => {
      const date = new Date(event.event_date);
      const monthYear = date.toLocaleDateString("fi-FI", {
        year: "numeric",
        month: "long",
      });

      if (!grouped[monthYear]) {
        grouped[monthYear] = [];
      }
      grouped[monthYear].push(event);
    });

    return grouped;
  };

  // Get grouped events
  const groupedEvents = groupEventsByMonth(filteredEvents);

  // Get unique venues for the dropdown
  const getUniqueVenues = (events) => {
    const venues = [...new Set(events.map((event) => event.venue_name))];
    return venues.sort();
  };

  return (
    <Box
      component="main"
      sx={{
        borderRadius: 2,
        display: "flex",
        justifyContent: "center",
        bgcolor: theme.background,
        minHeight: "100vh",
        py: 4,
      }}
    >
      <Paper
        elevation={5}
        sx={{
          background: theme.surface,
          borderRadius: 5,
          fontFamily: "Montserrat, Arial, sans-serif",
          maxWidth: 700,
          width: "100%",
          m: { xs: 1, sm: 2, md: 4 },
          p: { xs: 2, sm: 3, md: 4 },
        }}
      >
        <Box sx={{ textAlign: "center", mb: 4 }}>
          <h1
            style={{
              fontFamily: "Montserrat, sans-serif",
              fontSize: "2.3rem",
              fontWeight: 800,
              color: theme.text,
              margin: 0,
              marginBottom: "10px",
            }}
          >
            Keikkakalenteri
          </h1>
          <Box
            sx={{
              width: "280px",
              height: "4px",
              background: `linear-gradient(90deg, ${theme.primary}, ${theme.accent})`,
              margin: "0 auto",
              borderRadius: "2px",
            }}
          />
        </Box>

        <FormControl
          sx={{
            mt: 5,
            width: "100%",
            maxWidth: 300,
          }}
        >
          <InputLabel>Tapahtumapaikka</InputLabel>
          <Select
            value={selectedVenue}
            label="Tapahtumapaikka"
            onChange={(e) => setSelectedVenue(e.target.value)}
          >
            <MenuItem value="all">Kaikki</MenuItem>
            {getUniqueVenues(events).map((venue) => (
              <MenuItem key={venue} value={venue}>
                {venue}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        {/* Display grouped events by month */}
        {Object.keys(groupedEvents).length > 0 ? (
          Object.entries(groupedEvents).map(([month, monthEvents]) => (
            <Box component="section" key={month} sx={{ mb: 3 }}>
              <MonthComponent month={month} theme={theme} />
              {monthEvents.map((event) => (
                <Event key={event.event_id} props={event} theme={theme} />
              ))}
            </Box>
          ))
        ) : (
          <Box sx={{ textAlign: "center", color: theme.textSecondary, py: 4 }}>Ei keikkoja</Box>
        )}
      </Paper>
    </Box>
  );
};

export { Home, theme };
