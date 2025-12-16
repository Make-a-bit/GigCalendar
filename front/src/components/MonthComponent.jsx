import React from "react";
import { Box, Paper } from "@mui/material";

const MonthComponent = ({ month, theme }) => {
  const capitalizedMonth = month.charAt(0).toUpperCase() + month.slice(1);

  return (
    <Box component="header" sx={{ mt: 3, mb: 2 }}>
      <h2
        style={{
          backgroundColor: theme.primary,
          color: "#ffffff",
          fontSize: "1.5rem",
          fontWeight: 600,
          padding: "16px 24px",
          margin: 0,
          borderRadius: "8px",
          boxShadow: "0px 5px 5px -2px rgba(0,0,0,0.2), 0px 5px 5px 2px rgba(0,0,0,0.14)",
        }}
      >
        {capitalizedMonth}
      </h2>
    </Box>
  );
};

export default MonthComponent;
