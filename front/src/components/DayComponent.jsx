import React from "react";
import { Box } from "@mui/material";

const DayComponent = ({ day, time, theme }) => {
  return (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        width: 60,
        minHeight: 60,
        border: `2px solid ${theme.primary}`,
        borderRadius: 2,
        p: 1,
      }}
    >
      <Box
        sx={{
          fontSize: "1.75rem",
          fontWeight: "bold",
          color: theme.primary,
          lineHeight: 1,
        }}
      >
        {day}
      </Box>
      <Box
        sx={{
          fontSize: "0.75rem",
          color: theme.textSecondary,
          mt: 0.5,
        }}
      >
        {time === "00:00" ? "klo ?" : time}
      </Box>
    </Box>
  );
};

export default DayComponent;
