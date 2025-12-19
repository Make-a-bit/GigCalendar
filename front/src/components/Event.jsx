import React from "react";
import { Box } from "@mui/material";
import DayComponent from "./DayComponent";
import ArtistComponent from "./ArtistComponent";
import LocationComponent from "./LocationComponent";

const Event = ({ props, theme }) => {
  // Parse the date to get day number
  const dateStr = props.event_date;
  const [datePart, timePart] = dateStr.split("T");
  const dayNum = datePart.split("-")[2];
  const [hours, minutes] = timePart.split(":");

  const day = parseInt(dayNum, 10);
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
      {props.event_has_showtime ? (
        <DayComponent day={day} time={formattedTime} theme={theme} />
      ) : (
        <DayComponent day={day} time={null} theme={theme} />
      )}

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
