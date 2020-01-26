# ImagePortfolio

A tool for deploying and displaying my photography portfolio.

## Roadmap/wishlist

### Core

- [x] Creating photos in multiple sizes based on need
- [x] Syncing all images to S3
- [ ] Set up pipeline so that sync and upload happens on each push
    - Potentially use GitHub Actions
- [ ] On build push JSON file with sizing information to S3

### Displaying photos

- [ ] Set up NextJS static site which uses JSON containing sizing information to render all available srcsets
- [ ] During backend deployment send a build webhook to Netlify to trigger a build so that the static site always has the latest correct data
- [ ] Create list/grid of all (current) photos in static site
- [ ] Create single photo page
- [ ] For each photo in the static site, responsively render all image resolutions in srcset attribute

