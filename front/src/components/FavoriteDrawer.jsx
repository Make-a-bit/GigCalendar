import React from "react";
import { Drawer, Box, IconButton, Typography } from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import FavoriteArtists from "./favoriteArtists.jsx";

const FavoriteDrawer = ({ open, onClose, theme, onFavoritesChange }) => {
  return (
    <Drawer
      anchor="right"
      open={open}
      onClose={onClose}
      sx={{
        "& .MuiDrawer-paper": {
          maxWidth: "100%",
          backgroundColor: theme.background,
          p: 3,
        },
      }}
    >
      {/* Header with close button */}
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          mb: 3,
        }}
      >
        <Typography
          variant="h5"
          sx={{
            color: theme.text,
            fontWeight: 700,
            fontFamily: "Montserrat, sans-serif",
          }}
        >
          Suosikkiartistit
        </Typography>
        <IconButton onClick={onClose} sx={{ color: theme.text }}>
          <CloseIcon />
        </IconButton>
      </Box>

      {/* Favorite Artists Manager */}
      <FavoriteArtists theme={theme} onFavoritesChange={onFavoritesChange} />

      {/* Optional: Instructions */}
      <Box sx={{ mt: 3, p: 2, backgroundColor: theme.surface, borderRadius: 2 }}>
        <Typography variant="body2" sx={{ color: theme.textSecondary }}>
          💡 Lisää suosikkiartistit listalle. Tapahtumat, joissa he esiintyvät, korostetaan.
        </Typography>
      </Box>
    </Drawer>
  );
};

export default FavoriteDrawer;
