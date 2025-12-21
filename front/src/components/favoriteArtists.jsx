import React, { useState, useEffect } from "react";
import { Box, Button, Chip, Paper, TextField, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";

const FavoriteArtists = ({ theme, onFavoritesChange }) => {
  const [favorites, setFavorites] = useState([]);
  const [inputValue, setInputValue] = useState("");

  // Load favorites from localStorage on mount
  useEffect(() => {
    const stored = localStorage.getItem("favoriteArtists");
    if (stored) {
      try {
        const parsed = JSON.parse(stored);
        setFavorites(parsed);
        onFavoritesChange(parsed);
      } catch (error) {
        console.error("Error loading favorites:", error);
        setFavorites([]);
      }
    }
  }, [onFavoritesChange]);

  // Save to localStorage whenever favorites change
  const saveFavorites = (newFavorites) => {
    localStorage.setItem("favoriteArtists", JSON.stringify(newFavorites));
    setFavorites(newFavorites);
    onFavoritesChange(newFavorites); // Notify parent
  };

  const handleAddFavorite = () => {
    const trimmed = inputValue.trim();

    if (!trimmed) {
      return;
    }

    const isDuplicate = favorites.some((fav) => fav.toLowerCase() === trimmed.toLowerCase());

    if (isDuplicate) {
      alert("Artisti on jo suosikeissa!");
      return;
    }

    const newFavorites = [...favorites, trimmed];
    saveFavorites(newFavorites);
    setInputValue("");
  };

  const handleRemoveFavorite = (artistToRemove) => {
    const newFavorites = favorites.filter((fav) => fav !== artistToRemove);
    saveFavorites(newFavorites);
  };

  const handleKeyPress = (e) => {
    if (e.key === "Enter") {
      e.preventDefault();
      handleAddFavorite();
    } else if (e.key === "Escape") {
      setInputValue("");
    }
  };

  return (
    <Paper
      elevation={2}
      sx={{
        p: 2,
        mb: 3,
        background: theme.surface,
        borderRadius: 2,
      }}
    >
      <Typography
        variant="h6"
        sx={{
          color: theme.text,
          mb: 2,
          fontWeight: 600,
        }}
      >
        Suosikkiartistit
      </Typography>

      {/* Input field to add new favorites */}
      <Box sx={{ display: "flex", gap: 1, mb: 2 }}>
        <TextField
          fullWidth
          size="small"
          placeholder="Lisää artisti..."
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          onKeyDown={handleKeyPress}
          sx={{
            "& .MuiOutlinedInput-root": {
              color: theme.text,
            },
          }}
        />

        <Button
          variant="contained"
          onClick={handleAddFavorite}
          startIcon={<AddIcon />}
          sx={{
            backgroundColor: theme.primary,
            "&:hover": {
              backgroundColor: theme.accent,
            },
          }}
        >
          Lisää
        </Button>
      </Box>

      {/* Display favorite artists as chips */}
      <Box sx={{ display: "flex", flexWrap: "wrap", gap: 1 }}>
        {favorites.length === 0 ? (
          <Typography
            variant="body2"
            sx={{
              color: theme.textSecondary,
              fontStyle: "italic",
            }}
          >
            Ei suosikkiartisteja
          </Typography>
        ) : (
          favorites.map((artist, index) => (
            <Chip
              key={index}
              label={artist}
              onDelete={() => handleRemoveFavorite(artist)}
              deleteIcon={<DeleteIcon />}
              sx={{
                backgroundColor: theme.primary,
                color: "#fff",
                "& .MuiChip-deleteIcon": {
                  color: "#fff",
                  "&:hover": {
                    color: "#ffcccc",
                  },
                },
              }}
            />
          ))
        )}
      </Box>
    </Paper>
  );
};

export default FavoriteArtists;
