import { downloadBlob } from '../../shared/utils/blob-download.util';

describe('downloadBlob', () => {
  it('creates an object URL and triggers a download link click', () => {
    const click = jasmine.createSpy('click');
    const remove = jasmine.createSpy('remove');
    spyOn(URL, 'createObjectURL').and.returnValue('blob:test');
    spyOn(URL, 'revokeObjectURL');
    spyOn(document, 'createElement').and.returnValue({
      click,
      remove,
      rel: '',
      href: '',
      download: '',
    } as unknown as HTMLAnchorElement);
    spyOn(document.body, 'appendChild');

    downloadBlob(new Blob(['x']), 'report.pdf');

    expect(URL.createObjectURL).toHaveBeenCalled();
    expect(click).toHaveBeenCalled();
    expect(remove).toHaveBeenCalled();
  });
});
