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
      {price == null || price === "Ei hintatietoa"
        ? `${location}: Tarkista liput`
        : `${location}: ${price}`}
    </div>
  );
};

export default LocationComponent;
