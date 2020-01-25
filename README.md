# ImagePortfolio

A tool for deploying and displaying my photography portfolio.

## Roadmap/wishlist

### Core

- [x] Creating photos in multiple sizes based on need
- [x] Syncing all images to S3

### Displaying photos

- [ ] Show list/grid of all (current) photos in a static site
    - Might need to expose an API (potentially consisting of static JSON stored in S3) to supply to a static site renderer
    - Potentially include static site generator (Next.JS) inside the same repo so that the static site always gets built when a deploy happens so that the backend and frontend are never out of sync
- [ ] For each photo in the static site, responsively render all image resolutions in srcset attribute

