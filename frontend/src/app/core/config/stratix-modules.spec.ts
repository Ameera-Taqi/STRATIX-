import { canAccessModule, STRATIX_MODULES } from './stratix-modules';

describe('canAccessModule CMS fail-closed', () => {
  const settings = STRATIX_MODULES.find((m) => m.code === 'SETTINGS')!;

  it('denies company admin when CMS map is empty', () => {
    expect(canAccessModule(settings, 'ORG_ADMIN', 'read', {})).toBeFalse();
    expect(canAccessModule(settings, 'ADMIN', 'write', {})).toBeFalse();
  });

  it('denies company admin when CMS map is null', () => {
    expect(canAccessModule(settings, 'ORG_ADMIN', 'read', null)).toBeFalse();
  });

  it('honors CMS visible/writable flags when present', () => {
    const map = { SETTINGS: { visible: true, writable: false } };
    expect(canAccessModule(settings, 'ORG_ADMIN', 'read', map)).toBeTrue();
    expect(canAccessModule(settings, 'ORG_ADMIN', 'write', map)).toBeFalse();
  });

  it('keeps SUPER_ADMIN open regardless of CMS', () => {
    expect(canAccessModule(settings, 'SUPER_ADMIN', 'write', {})).toBeTrue();
  });
});
