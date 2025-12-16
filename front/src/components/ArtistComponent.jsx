import React from "react";

const ArtistComponent = ({ artist, theme }) => {
  return (
    <div
      style={{
        fontSize: "1.125rem",
        fontWeight: 600,
        color: theme.text,
        marginBottom: "0.25rem",
      }}
    >
      {artist}
    </div>
  );
};
export default ArtistComponent;
