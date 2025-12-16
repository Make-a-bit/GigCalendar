import React from "react";
import { Box } from "@mui/material";
import DayComponent from "./DayComponent";
import ArtistComponent from "./ArtistComponent";
import LocationComponent from "./LocationComponent";

const Event = ({ props, theme }) => {
  // Parse the date to get day number
  const date = new Date(props.event_date);
  const day = date.getDate();

  // Parse the time to get hours and minutes
  const hours = date.getHours().toString().padStart(2, "0");
  const minutes = date.getMinutes().toString().padStart(2, "0");
  const formattedTime = `${hours}:${minutes}`;

  return (
    <Box
      component="article"
      sx={{
        display: "flex",
        mb: 1,
        borderBottom: `1px solid ${theme.primary}`,
        pb: 1,
        "&:last-child": {
          borderBottom: "none",
        },
      }}
    >
      <DayComponent day={day} time={formattedTime} theme={theme} />
      <Box
        sx={{
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          ml: 2,
          flex: 1,
        }}
      >
        <ArtistComponent artist={props.event_artist} theme={theme} />
        <LocationComponent location={props.venue_name} price={props.event_price} theme={theme} />
      </Box>
    </Box>
  );
};

export default Event;
