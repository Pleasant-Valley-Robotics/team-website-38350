import { getEntry } from 'astro:content';

export async function getTeam() {
  const team = await getEntry('team', 'team');
  if (!team) throw new Error('src/content/team.md is missing');
  return team;
}

export const PV_SITE = 'https://www.firstpvrobotics.com';
