import React from "react";

const LocationComponent = ({ location, price, theme }) => {
  return (
    <div
      style={{
        fontSize: "0.875rem",
        fontStyle: "italic",
        color: theme.textSecondary,
      }}
    >
      {price ? `${location}: ${price}` : location}
    </div>
  );
};

export default LocationComponent;
