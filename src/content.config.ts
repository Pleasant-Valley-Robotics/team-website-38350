import { defineCollection, z } from 'astro:content';
import { glob } from 'astro/loaders';

// All of the team's facts live in src/content/team.md.
// If the build fails with a message about a field, the error names the line to fix.
const team = defineCollection({
  loader: glob({ pattern: 'team.md', base: './src/content' }),
  schema: z.object({
    name: z.string(),
    shortName: z.string(),
    program: z.enum(['FTC', 'FRC']),
    number: z.number(),
    founded: z.string().optional(),
    tagline: z.string(),
    logo: z.string(),
    // Background behind the logo: white suits most logos; use a dark color for logos drawn on black.
    logoBg: z.string().default('#ffffff'),
    photo: z.string().optional(),
    photoAlt: z.string().optional(),
    accent: z.string(),
    email: z.string().optional(),
    socials: z.array(z.object({ label: z.string(), url: z.string().url() })).default([]),
    highlights: z.array(z.string()).default([]),
    sections: z
      .array(z.object({ title: z.string(), items: z.array(z.object({ title: z.string(), body: z.string() })) }))
      .default([]),
    outreach: z.array(z.object({ title: z.string(), body: z.string() })).default([]),
  }),
});

export const collections = { team };
