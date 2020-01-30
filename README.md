# ImagePortfolio

A tool for deploying and displaying my photography portfolio.

## Roadmap/wishlist

### Core

- [x] Creating photos in multiple sizes based on need
- [x] Syncing all images to S3
- [x] Set up pipeline so that sync and upload happens on each push
    - Potentially use GitHub Actions
- [x] On build push JSON file with sizing information to S3
- [x] Support albums
- [ ] Support album info in text/md/json file
- [ ] Add height and width information so portrait photos can be displayed properly

### Displaying photos

- [ ] Set up NextJS static site which uses JSON containing sizing information to render all available srcsets
- [ ] During backend deployment send a push webhook to Netlify to trigger a build so that the static site always has the latest correct data
- [ ] Create list/grid of all (current) photos and albums in static site
- [ ] Create single photo page
    - [ ] Photo itself
    - [ ] Caption and optional additional text
    - [ ] Adequate margin
    - [ ] Fullscreen mode
- [ ] Create album page
- [ ] For each photo in the static site, responsively render all image resolutions in srcset attribute
- [ ] Display photo metadata (ISO, SS, aperture, etc)
- [ ] Group photos into albums
- [ ] Add tags to photos and/or albums

### Tech & configuration

- [x] Parameterise AWS credentials as env variables
- [x] Parameterise bucket name as env variable
- [ ] Parameterise Netlify site URL for triggering builds
- [ ] Use ImageMagick for image conversions
