import React from "react";
import { Box, Paper } from "@mui/material";

const LocationComponent = ({ location, price, theme }) => {
  return (
    <div
      style={{
        fontSize: "0.875rem",
        fontStyle: "italic",
        color: theme.textSecondary,
      }}
    >
      {location} / {price}
    </div>
  );
};

export default LocationComponent;
